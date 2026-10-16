using System.Globalization;
using System.Security.Claims;
using AspNetCoreSkeletonApplication.Common.Errors;
using AspNetCoreSkeletonApplication.Common.Persistence.Identity;
using AspNetCoreSkeletonApplication.Common.Web.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;

namespace AspNetCoreSkeletonApplication.Common.Web;

/// <summary>
/// 소셜 로그인 — <c>/api/auth/external</c>. <b>연결된 계정만 로그인한다</b>(자동 가입 없음). 연결은 로그인한 사용자가 계정 화면에서 한다.
/// <list type="bullet">
/// <item>시작(<c>login</c> · <c>link</c>)은 POST 다 — 브라우저 폼 이동으로 부른다. cross-site POST 는 Fetch Metadata 가 막으므로
/// 다른 사이트가 로그인 · 연결을 몰래 시작할 수 없다(로그인 CSRF · 원치 않는 연결 방지).</item>
/// <item>state · correlation 은 OAuth 핸들러가 검증한다. 공급자 콜백은 핸들러의 <c>signin-{공급자}</c> 가 받아 외부 쿠키에 담고,
/// <see cref="CallbackAsync"/> 가 그것을 세션으로 바꾸거나 계정에 연결한 뒤 SPA 화면으로 보낸다(토큰을 URL 에 싣지 않는다).</item>
/// <item>콜백은 공급자에서 돌아오는 cross-site 이동이라 SameSite=Strict 세션 쿠키가 실리지 않는다 — 그래서 연결할 사용자는 시작할 때
/// 보호된 state(<c>link-user</c>)에 담아 둔다.</item>
/// </list>
/// </summary>
public static class ExternalLoginEndpoints
{
    private const string CallbackPath = "/api/auth/external/callback";
    private const string ModeKey = "mode";
    private const string ReturnUrlKey = "returnUrl";
    private const string LinkUserKey = "link-user";

    public static IEndpointRouteBuilder MapSkeletonExternalLoginEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var external = endpoints.MapGroup("/api/auth/external");
        external.MapGet("/providers", ListProvidersAsync).AllowAnonymous();
        external.MapPost("/{provider}/login", StartLoginAsync).AllowAnonymous();
        external.MapPost("/{provider}/link", StartLinkAsync).RequireAuthorization();
        external.MapGet("/callback", CallbackAsync).AllowAnonymous();
        external.MapGet("/logins", ListLoginsAsync).RequireAuthorization();
        external.MapDelete("/{provider}", UnlinkAsync).RequireAuthorization();
        return endpoints;
    }

    private static async Task<Ok<ExternalProviderResponse[]>> ListProvidersAsync(SignInManager<ApplicationUser> signInManager) =>
        TypedResults.Ok((await ProvidersAsync(signInManager))
            .Select(scheme => new ExternalProviderResponse(scheme.Name, scheme.DisplayName ?? scheme.Name))
            .ToArray());

    /// <summary>켜진 소셜 공급자 — 등록된 인증 스킴 중 공급자 이름인 것만(쿠키 · 토큰 스킴은 빼고).</summary>
    private static async Task<IEnumerable<AuthenticationScheme>> ProvidersAsync(SignInManager<ApplicationUser> signInManager) =>
        (await signInManager.GetExternalAuthenticationSchemesAsync())
            .Where(scheme => ExternalLoginServiceCollectionExtensions.Providers.Contains(scheme.Name));

    /// <summary>공급자 로그인 화면으로 보낸다. 끝나면 <paramref name="returnUrl"/>(같은 출처 경로만)로 돌아온다.</summary>
    private static async Task<IResult> StartLoginAsync(string provider, string? returnUrl, SignInManager<ApplicationUser> signInManager)
    {
        if (!await IsProviderAsync(signInManager, provider))
        {
            return TypedResults.NotFound();
        }

        var properties = signInManager.ConfigureExternalAuthenticationProperties(provider, CallbackPath);
        properties.Items[ModeKey] = "login";
        properties.Items[ReturnUrlKey] = SafeLocalPath(returnUrl);
        return TypedResults.Challenge(properties, [provider]);
    }

    /// <summary>로그인한 사용자의 계정에 공급자 계정을 연결한다. 끝나면 계정 화면(<c>/account</c>)으로 돌아온다.</summary>
    private static async Task<IResult> StartLinkAsync(string provider, ClaimsPrincipal user, SignInManager<ApplicationUser> signInManager)
    {
        if (!await IsProviderAsync(signInManager, provider))
        {
            return TypedResults.NotFound();
        }

        var userId = UserClaims.IdOf(user) ?? throw new InvalidOperationException("로그인한 사용자에게 ID 클레임이 없다");
        var properties = signInManager.ConfigureExternalAuthenticationProperties(provider, CallbackPath);
        properties.Items[ModeKey] = "link";
        properties.Items[LinkUserKey] = userId.ToString(CultureInfo.InvariantCulture);
        return TypedResults.Challenge(properties, [provider]);
    }

    private static async Task<RedirectHttpResult> CallbackAsync(
        HttpContext httpContext, SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager)
    {
        var info = await signInManager.GetExternalLoginInfoAsync();
        await httpContext.SignOutAsync(IdentityConstants.ExternalScheme);
        if (info is null)
        {
            return TypedResults.Redirect("/login?error=EXTERNAL_LOGIN_FAILED");
        }

        var properties = info.AuthenticationProperties ?? new AuthenticationProperties();
        if (properties.Items.TryGetValue(ModeKey, out var mode) && mode == "link"
            && long.TryParse(properties.Items.TryGetValue(LinkUserKey, out var linkUser) ? linkUser : null, CultureInfo.InvariantCulture, out var userId))
        {
            return TypedResults.Redirect(await LinkAsync(userManager, userId, info));
        }

        // 2단계 인증을 켠 계정은 소셜 로그인 뒤에도 두 번째 단계를 거친다(기억한 브라우저면 바로 로그인).
        var result = await signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: false);
        var returnUrl = SafeLocalPath(properties.Items.TryGetValue(ReturnUrlKey, out var requested) ? requested : null);
        if (result.Succeeded)
        {
            return TypedResults.Redirect(returnUrl);
        }

        if (result.RequiresTwoFactor)
        {
            return TypedResults.Redirect($"/login?step=two-factor&returnUrl={Uri.EscapeDataString(returnUrl)}");
        }

        // NotAllowed — 연결은 됐지만 이메일을 확인하지 않은 계정(RequireConfirmedEmail).
        var error = result.IsLockedOut ? ErrorCode.AccountLocked.Code
            : result.IsNotAllowed ? ErrorCode.EmailNotConfirmed.Code
            : ErrorCode.ExternalLoginNotLinked.Code;
        return TypedResults.Redirect($"/login?error={error}&provider={Uri.EscapeDataString(info.LoginProvider)}");
    }

    /// <summary>연결 결과에 따라 계정 화면 주소를 정한다. 다른 사용자에게 이미 연결된 공급자 계정이면 연결하지 않는다.</summary>
    private static async Task<string> LinkAsync(UserManager<ApplicationUser> userManager, long userId, ExternalLoginInfo info)
    {
        var provider = Uri.EscapeDataString(info.LoginProvider);
        var user = await userManager.FindByIdAsync(userId.ToString(CultureInfo.InvariantCulture));
        if (user is null)
        {
            return "/login?error=EXTERNAL_LOGIN_FAILED";
        }

        var owner = await userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
        if (owner is not null)
        {
            return owner.Id == user.Id ? $"/account?linked={provider}" : $"/account?error={ErrorCode.ExternalLoginInUse.Code}&provider={provider}";
        }

        if ((await userManager.GetLoginsAsync(user)).Any(login => login.LoginProvider == info.LoginProvider))
        {
            return $"/account?error={ErrorCode.ExternalLoginInUse.Code}&provider={provider}";
        }

        var added = await userManager.AddLoginAsync(user, info);
        return added.Succeeded ? $"/account?linked={provider}" : "/account?error=EXTERNAL_LOGIN_FAILED";
    }

    private static async Task<Ok<ExternalLoginResponse[]>> ListLoginsAsync(ClaimsPrincipal principal, UserManager<ApplicationUser> userManager)
    {
        var user = await userManager.GetUserAsync(principal) ?? throw new InvalidOperationException("로그인한 사용자가 없다");
        return TypedResults.Ok((await userManager.GetLoginsAsync(user))
            .Select(login => new ExternalLoginResponse(login.LoginProvider, login.ProviderDisplayName ?? login.LoginProvider))
            .ToArray());
    }

    /// <summary>연결을 끊는다. 비밀번호도 다른 연결도 없으면 마지막 로그인 수단이라 409 다.</summary>
    private static async Task<Results<NoContent, NotFound>> UnlinkAsync(
        string provider, ClaimsPrincipal principal, UserManager<ApplicationUser> userManager)
    {
        var user = await userManager.GetUserAsync(principal) ?? throw new InvalidOperationException("로그인한 사용자가 없다");
        var logins = await userManager.GetLoginsAsync(user);
        var login = logins.FirstOrDefault(candidate => candidate.LoginProvider == provider);
        if (login is null)
        {
            return TypedResults.NotFound();
        }

        if (logins.Count == 1 && !await userManager.HasPasswordAsync(user) && (await userManager.GetPasskeysAsync(user)).Count == 0)
        {
            throw new LastLoginMethodException("로그인할 방법이 이것 하나뿐이라 연결을 끊을 수 없어요. 비밀번호를 정하거나 다른 계정 · 패스키를 먼저 연결해 주세요.");
        }

        await userManager.RemoveLoginAsync(user, login.LoginProvider, login.ProviderKey);
        return TypedResults.NoContent();
    }

    private static async Task<bool> IsProviderAsync(SignInManager<ApplicationUser> signInManager, string provider) =>
        (await ProvidersAsync(signInManager)).Any(scheme => scheme.Name == provider);

    /// <summary>같은 출처의 경로만 — 외부 주소로 보내는 열린 리다이렉트를 막는다.</summary>
    /// <summary>
    /// 같은 출처의 경로만 돌려준다 — 아니면 <c>/</c>. 공백 · 제어 문자가 있으면 거절한다: 브라우저는 Location 의 탭 · 줄바꿈을 지우므로
    /// <c>/\t/evil.example</c> 이 <c>//evil.example</c>(다른 사이트)로 간다.
    /// </summary>
    internal static string SafeLocalPath(string? value) =>
        value is { Length: > 0 } && value[0] == '/' && !value.StartsWith("//", StringComparison.Ordinal) && !value.StartsWith("/\\", StringComparison.Ordinal)
        && !value.Any(character => character <= ' ' || character == '\x7f')
            ? value
            : "/";

    public sealed record ExternalProviderResponse(string Name, string DisplayName);

    public sealed record ExternalLoginResponse(string Provider, string DisplayName);
}
