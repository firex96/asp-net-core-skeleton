using System.Globalization;
using System.Security.Claims;
using AspNetCoreSkeletonApplication.Common.Persistence.Identity;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace AspNetCoreSkeletonApplication.Common.Web.OAuth;

/// <summary>
/// OAuth 서버 엔드포인트의 요청 처리(OpenIddict passthrough) — 요청 검증은 OpenIddict 가 끝낸 뒤 여기로 온다.
/// <list type="bullet">
/// <item><b>authorize</b> — 세션 쿠키로 로그인을 확인한다. 없으면 SPA <c>/login?returnUrl=…</c> 로 보내고, SPA 가 로그인 뒤 같은 주소로 다시 이동한다
/// (그 이동은 same-site 라 SameSite=Strict 세션 쿠키가 실린다). 동의가 필요한 클라이언트(<c>Explicit</c>)는 SPA <c>/consent</c> 로 보내고,
/// 동의 화면이 같은 요청을 <c>consent=accept|deny</c> 와 함께 POST 한다. 허락은 영구 authorization 으로 저장해 다시 묻지 않는다.
/// <c>prompt</c> 는 <c>none</c>(묻지 않고 실패) · <c>login</c>(세션을 끊고 다시 로그인) · <c>consent</c>(이미 허락했거나 Implicit 이어도 동의 화면)를 처리한다.</item>
/// <item><b>token</b> — 인가 코드 · 리프레시 토큰은 사용자가 아직 로그인할 수 있는지 다시 본다(잠김 · 삭제면 거절). 클라이언트 자격 증명은
/// 주체를 <c>client:{clientId}</c> 로 둔다 — 숫자가 아니므로 사용자 ID 로 오인되지 않는다.</item>
/// </list>
/// </summary>
public static class OAuthServerEndpoints
{
    public const string ClientSubjectPrefix = "client:";

    /// <summary>다른 출처의 브라우저 앱에 여는 CORS 정책 — 구성 <c>Authentication:OAuthServer:AllowedOrigins</c>.</summary>
    public const string CorsPolicy = "OAuthServer";

    /// <summary>
    /// OAuth 엔드포인트(<c>/connect/*</c> · <c>/.well-known/*</c>)에만 CORS 를 건다. OpenIddict 는 이 요청을 라우팅된 엔드포인트가 아니라 인증 단계에서
    /// 처리하므로(탐색 문서 · JWKS 는 거기서 끝난다) 경로로 고르고, <c>UseAuthentication</c> 앞에 둔다.
    /// </summary>
    public static IApplicationBuilder UseSkeletonOAuthServerCors(this IApplicationBuilder app) =>
        app.UseWhen(
            context => context.Request.Path.StartsWithSegments("/connect") || context.Request.Path.StartsWithSegments("/.well-known"),
            branch => branch.UseCors(CorsPolicy));

    public static IEndpointRouteBuilder MapSkeletonOAuthServerEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapMethods("/connect/authorize", [HttpMethods.Get, HttpMethods.Post], AuthorizeAsync).AllowAnonymous();
        endpoints.MapPost("/connect/token", TokenAsync).AllowAnonymous();
        endpoints.MapMethods("/connect/userinfo", [HttpMethods.Get, HttpMethods.Post], UserInfoAsync).AllowAnonymous();
        endpoints.MapMethods("/connect/endsession", [HttpMethods.Get, HttpMethods.Post], EndSessionAsync).AllowAnonymous();
        endpoints.MapGet("/api/oauth/consent", ConsentAsync).RequireAuthorization();
        return endpoints;
    }

    private static async Task<IResult> AuthorizeAsync(
        HttpContext context,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IOpenIddictApplicationManager applicationManager,
        IOpenIddictAuthorizationManager authorizationManager)
    {
        var request = context.GetOpenIddictServerRequest() ?? throw new InvalidOperationException("OpenIddict 요청을 읽지 못했다");
        var session = await context.AuthenticateAsync(IdentityConstants.ApplicationScheme);
        var user = session.Succeeded ? await userManager.GetUserAsync(session.Principal) : null;
        if (request.HasPromptValue(PromptValues.Login))
        {
            // 다시 로그인하게 한다 — 세션을 끊고, 돌아올 주소에서는 login 을 뺀다(로그인한 뒤 같은 요청이 또 세션을 끊지 않게).
            if (user is not null)
            {
                await signInManager.SignOutAsync();
            }

            return TypedResults.Redirect($"/login?returnUrl={Uri.EscapeDataString(AuthorizeUrlOf(request, withoutPrompt: PromptValues.Login))}");
        }

        if (user is null || !await signInManager.CanSignInAsync(user))
        {
            return request.HasPromptValue(PromptValues.None)
                ? Forbid(OpenIddictConstants.Errors.LoginRequired, "로그인이 필요해요.")
                : TypedResults.Redirect($"/login?returnUrl={Uri.EscapeDataString(AuthorizeUrlOf(request))}");
        }

        var application = await applicationManager.FindByClientIdAsync(request.ClientId!)
                          ?? throw new InvalidOperationException("클라이언트를 찾지 못했다");
        var applicationId = await applicationManager.GetIdAsync(application);
        var subject = user.Id.ToString(CultureInfo.InvariantCulture);
        var scopes = request.GetScopes();
        var authorizations = await authorizationManager.FindAsync(
            subject: subject, client: applicationId, status: Statuses.Valid, type: AuthorizationTypes.Permanent, scopes: scopes).ToListAsync();

        var decision = HttpMethods.IsPost(context.Request.Method) ? context.Request.Form["consent"].ToString() : string.Empty;
        if (decision == "deny")
        {
            return Forbid(OpenIddictConstants.Errors.AccessDenied, "사용자가 접근을 허용하지 않았어요.");
        }

        var needsConsent = decision != "accept"
                           && (request.HasPromptValue(PromptValues.Consent)
                               || (await applicationManager.GetConsentTypeAsync(application) == ConsentTypes.Explicit && authorizations.Count == 0));
        if (needsConsent)
        {
            return request.HasPromptValue(PromptValues.None)
                ? Forbid(OpenIddictConstants.Errors.ConsentRequired, "사용자의 동의가 필요해요.")
                : TypedResults.Redirect($"/consent?{QueryOf(request)}");
        }

        var identity = await IdentityOfAsync(userManager, user, scopes);
        var authorization = authorizations.LastOrDefault()
                            ?? await authorizationManager.CreateAsync(
                                identity: identity, subject: subject, client: applicationId!, type: AuthorizationTypes.Permanent, scopes: scopes);
        identity.SetAuthorizationId(await authorizationManager.GetIdAsync(authorization));
        return TypedResults.SignIn(new ClaimsPrincipal(identity), authenticationScheme: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private static async Task<IResult> TokenAsync(
        HttpContext context, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
    {
        var request = context.GetOpenIddictServerRequest() ?? throw new InvalidOperationException("OpenIddict 요청을 읽지 못했다");
        if (request.IsClientCredentialsGrantType())
        {
            var client = new ClaimsIdentity(TokenValidationAuthenticationType, Claims.Name, Claims.Role);
            client.SetClaim(Claims.Subject, ClientSubjectPrefix + request.ClientId);
            client.SetClaim(Claims.Name, request.ClientId);
            client.SetScopes(request.GetScopes());
            client.SetDestinations(_ => [Destinations.AccessToken]);
            return TypedResults.SignIn(new ClaimsPrincipal(client), authenticationScheme: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        if (!request.IsAuthorizationCodeGrantType() && !request.IsRefreshTokenGrantType())
        {
            return Forbid(OpenIddictConstants.Errors.UnsupportedGrantType, "지원하지 않는 grant_type 이에요.");
        }

        var granted = await context.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        var user = granted.Principal?.GetClaim(Claims.Subject) is { } subject ? await userManager.FindByIdAsync(subject) : null;
        if (user is null || !await signInManager.CanSignInAsync(user) || await userManager.IsLockedOutAsync(user))
        {
            return Forbid(OpenIddictConstants.Errors.InvalidGrant, "이 토큰으로는 더 이상 로그인할 수 없어요.");
        }

        // 사용자 정보는 지금 값으로 다시 싣는다(이메일이 바뀌었을 수 있다). scope · 동의는 처음 받은 그대로다.
        var identity = await IdentityOfAsync(userManager, user, granted.Principal!.GetScopes());
        identity.SetAuthorizationId(granted.Principal!.GetAuthorizationId());
        return TypedResults.SignIn(new ClaimsPrincipal(identity), authenticationScheme: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private static async Task<IResult> UserInfoAsync(HttpContext context, UserManager<ApplicationUser> userManager)
    {
        var token = await context.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        var user = token.Principal?.GetClaim(Claims.Subject) is { } subject ? await userManager.FindByIdAsync(subject) : null;
        if (user is null)
        {
            return TypedResults.Challenge(authenticationSchemes: [OpenIddictServerAspNetCoreDefaults.AuthenticationScheme]);
        }

        var claims = new Dictionary<string, object>(StringComparer.Ordinal) { [Claims.Subject] = user.Id.ToString(CultureInfo.InvariantCulture) };
        if (token.Principal!.HasScope(Scopes.Email))
        {
            claims[Claims.Email] = user.Email!;
            claims[Claims.EmailVerified] = user.EmailConfirmed;
        }

        if (token.Principal!.HasScope(Scopes.Profile))
        {
            claims[Claims.Name] = user.UserName!;
        }

        return TypedResults.Ok(claims);
    }

    private static async Task<IResult> EndSessionAsync(SignInManager<ApplicationUser> signInManager)
    {
        await signInManager.SignOutAsync();
        return TypedResults.SignOut(new AuthenticationProperties { RedirectUri = "/" }, [OpenIddictServerAspNetCoreDefaults.AuthenticationScheme]);
    }

    /// <summary>동의 화면이 보여 줄 내용 — 클라이언트 이름과 요청한 scope 의 설명.</summary>
    private static async Task<Results<Ok<ConsentResponse>, NotFound>> ConsentAsync(
        string client_id, string? scope, IOpenIddictApplicationManager applicationManager)
    {
        var application = await applicationManager.FindByClientIdAsync(client_id);
        if (application is null)
        {
            return TypedResults.NotFound();
        }

        var scopes = (scope ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(name => new ConsentScope(name, OAuthScopes.DescriptionOf(name)))
            .ToArray();
        return TypedResults.Ok(new ConsentResponse(client_id, await applicationManager.GetDisplayNameAsync(application) ?? client_id, scopes));
    }

    /// <summary>
    /// 토큰에 실을 사용자 정보. 대상: 주체는 access · id 토큰, 이메일(+ <c>email_verified</c>) · 이름은 그 scope 를 받았을 때 둘 다.
    /// 로그인하려면 이메일을 확인해야 하므로(<c>RequireConfirmedEmail</c>) 토큰을 받는 사용자의 <c>email_verified</c> 는 true 다.
    /// </summary>
    private static async Task<ClaimsIdentity> IdentityOfAsync(UserManager<ApplicationUser> userManager, ApplicationUser user, IEnumerable<string> scopes)
    {
        var identity = new ClaimsIdentity(TokenValidationAuthenticationType, Claims.Name, Claims.Role);
        identity.SetClaim(Claims.Subject, user.Id.ToString(CultureInfo.InvariantCulture))
            .SetClaim(Claims.Email, await userManager.GetEmailAsync(user))
            .SetClaim(Claims.EmailVerified, await userManager.IsEmailConfirmedAsync(user))
            .SetClaim(Claims.Name, await userManager.GetUserNameAsync(user));
        identity.SetScopes(scopes);
        identity.SetDestinations(claim => claim.Type switch
        {
            Claims.Subject => [Destinations.AccessToken, Destinations.IdentityToken],
            Claims.Email or Claims.EmailVerified when identity.HasScope(Scopes.Email) => [Destinations.AccessToken, Destinations.IdentityToken],
            Claims.Name when identity.HasScope(Scopes.Profile) => [Destinations.AccessToken, Destinations.IdentityToken],
            _ => [],
        });
        return identity;
    }

    private const string TokenValidationAuthenticationType = "OAuth";

    private static ForbidHttpResult Forbid(string error, string description) =>
        TypedResults.Forbid(
            new AuthenticationProperties(new Dictionary<string, string?>
            {
                [OpenIddictServerAspNetCoreConstants.Properties.Error] = error,
                [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = description,
            }),
            [OpenIddictServerAspNetCoreDefaults.AuthenticationScheme]);

    private static string AuthorizeUrlOf(OpenIddictRequest request, string? withoutPrompt = null) =>
        $"/connect/authorize?{QueryOf(request, withoutPrompt)}";

    /// <summary>같은 authorize 요청의 쿼리 — 동의 결정(<c>consent</c>)은 빼고, <paramref name="withoutPrompt"/> 를 주면 <c>prompt</c> 에서 그 값을 뺀다.</summary>
    private static string QueryOf(OpenIddictRequest request, string? withoutPrompt = null) =>
        QueryString.Create(request.GetParameters()
            .Where(parameter => parameter.Key != "consent")
            .Select(parameter => new KeyValuePair<string, StringValues>(
                parameter.Key,
                parameter.Key == Parameters.Prompt && withoutPrompt is not null
                    ? string.Join(' ', ((string?)parameter.Value ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries).Where(value => value != withoutPrompt))
                    : (string?)parameter.Value))
            .Where(parameter => parameter.Key != Parameters.Prompt || !string.IsNullOrEmpty(parameter.Value)))
            .ToUriComponent().TrimStart('?');

    public sealed record ConsentResponse(string ClientId, string DisplayName, IReadOnlyList<ConsentScope> Scopes);

    public sealed record ConsentScope(string Name, string Description);
}
