using System.Globalization;
using AspNetCoreSkeletonApplication.Common.Errors;
using AspNetCoreSkeletonApplication.Common.Persistence.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;

namespace AspNetCoreSkeletonApplication.Common.Web;

/// <summary>
/// 패스키(WebAuthn) — 비밀번호 없는 로그인(<c>/api/auth/passkey</c>)과 내 계정의 패스키 관리(<c>/api/account/passkeys</c>). .NET Identity 의 내장 기능을 쓴다.
/// <list type="bullet">
/// <item>패스키는 그 자체로 다중 요소(기기 + 생체 · PIN)라 2단계 인증을 건너뛴다(Identity 의 <c>PasskeySignInAsync</c>). 2단계 인증의 두 번째 수단으로는 쓰지 않는다.</item>
/// <item>옵션 JSON 은 Identity 가 만든 그대로 돌려주고, 브라우저 세리머니 결과(<c>credential</c>)는 JSON 문자열로 받는다. 세리머니 상태(챌린지)는 Identity 가
/// 2단계 인증 대기 쿠키에 담는다.</item>
/// <item>RP ID 는 <c>App:PublicOrigin</c> 의 호스트다(<see cref="IdentityServiceCollectionExtensions"/>). 출처는 요청의 Origin 헤더와 세리머니의 출처가 같아야 한다.</item>
/// </list>
/// </summary>
public static class PasskeyEndpoints
{
    public static IEndpointRouteBuilder MapSkeletonPasskeyEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var login = endpoints.MapGroup("/api/auth/passkey").AllowAnonymous();
        login.MapPost("/options", RequestOptionsAsync);
        login.MapPost("/login", SignInAsync);

        var sessionOnly = new AuthorizationPolicyBuilder(IdentityConstants.ApplicationScheme).RequireAuthenticatedUser().Build();
        var account = endpoints.MapGroup("/api/account/passkeys").RequireAuthorization(sessionOnly);
        account.MapGet("", ListAsync);
        account.MapPost("/options", CreationOptionsAsync);
        account.MapPost("", AddAsync);
        account.MapPut("/{id}", RenameAsync);
        account.MapDelete("/{id}", RemoveAsync);
        return endpoints;
    }

    /// <summary>로그인용 요청 옵션 — 사용자를 정하지 않는다(브라우저가 이 사이트의 패스키 중에서 고르게 한다).</summary>
    private static async Task<ContentHttpResult> RequestOptionsAsync(SignInManager<ApplicationUser> signInManager) =>
        TypedResults.Content(await signInManager.MakePasskeyRequestOptionsAsync(null), "application/json");

    /// <summary>패스키로 로그인 — 204 + 세션 쿠키. 이메일을 확인하지 않은 계정은 403, 잠긴 계정은 423, 그 밖의 실패는 401 이다.</summary>
    private static async Task<Results<NoContent, ProblemHttpResult>> SignInAsync(PasskeyCredentialRequest request, SignInManager<ApplicationUser> signInManager)
    {
        var result = await signInManager.PasskeySignInAsync(request.Credential);
        if (result.Succeeded)
        {
            return TypedResults.NoContent();
        }

        if (result.IsNotAllowed)
        {
            throw new EmailNotConfirmedException("이메일 확인을 아직 마치지 않았어요. 가입할 때 받은 메일의 링크를 열어 주세요.");
        }

        return result.IsLockedOut
            ? TypedResults.Problem(statusCode: StatusCodes.Status423Locked, detail: "로그인에 여러 번 실패해서 계정이 잠겼어요. 30분 뒤에 다시 시도하거나 비밀번호를 재설정해 주세요.")
            : TypedResults.Problem(statusCode: StatusCodes.Status401Unauthorized, detail: "패스키로 로그인하지 못했어요. 이 사이트에 등록한 패스키인지 확인해 주세요.");
    }

    private static async Task<Ok<PasskeyResponse[]>> ListAsync(HttpContext context, UserManager<ApplicationUser> userManager)
    {
        var user = await CurrentUserAsync(context, userManager);
        var passkeys = await userManager.GetPasskeysAsync(user);
        return TypedResults.Ok(passkeys.OrderBy(passkey => passkey.CreatedAt).Select(ToResponse).ToArray());
    }

    /// <summary>등록용 생성 옵션 — 이미 등록한 패스키는 제외 목록에 들어간다(같은 인증기에 두 번 만들지 않는다).</summary>
    private static async Task<ContentHttpResult> CreationOptionsAsync(
        HttpContext context, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
    {
        var user = await CurrentUserAsync(context, userManager);
        var entity = new PasskeyUserEntity
        {
            Id = user.Id.ToString(CultureInfo.InvariantCulture),
            Name = user.Email!,
            DisplayName = user.Email!,
        };
        return TypedResults.Content(await signInManager.MakePasskeyCreationOptionsAsync(entity), "application/json");
    }

    /// <summary>브라우저가 만든 패스키를 확인해 저장한다. 확인에 실패하면 400 이다.</summary>
    private static async Task<Created<PasskeyResponse>> AddAsync(
        AddPasskeyRequest request, HttpContext context, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager,
        ILoggerFactory loggerFactory)
    {
        var user = await CurrentUserAsync(context, userManager);
        var attestation = await signInManager.PerformPasskeyAttestationAsync(request.Credential);
        if (!attestation.Succeeded)
        {
            loggerFactory.CreateLogger(typeof(PasskeyEndpoints)).LogWarning(attestation.Failure, "패스키 등록 확인 실패 — {Reason}", attestation.Failure?.Message);
            throw new ClientBadRequestException("패스키를 등록하지 못했어요. 다시 시도해 주세요.");
        }

        var passkey = attestation.Passkey;
        passkey.Name = NameOf(request.Name);
        EnsureSucceeded(await userManager.AddOrUpdatePasskeyAsync(user, passkey));
        var response = ToResponse(passkey);
        return TypedResults.Created($"/api/account/passkeys/{response.Id}", response);
    }

    private static async Task<NoContent> RenameAsync(
        string id, RenamePasskeyRequest request, HttpContext context, UserManager<ApplicationUser> userManager)
    {
        var user = await CurrentUserAsync(context, userManager);
        var passkey = await userManager.GetPasskeyAsync(user, DecodeId(id)) ?? throw new ItemNotFoundException("패스키를 찾지 못했어요.");
        passkey.Name = NameOf(request.Name);
        EnsureSucceeded(await userManager.AddOrUpdatePasskeyAsync(user, passkey));
        return TypedResults.NoContent();
    }

    /// <summary>패스키를 지운다. 남은 로그인 수단이 없어지면(비밀번호 · 소셜 연결 없이 마지막 패스키) 409 다.</summary>
    private static async Task<NoContent> RemoveAsync(string id, HttpContext context, UserManager<ApplicationUser> userManager)
    {
        var user = await CurrentUserAsync(context, userManager);
        var credentialId = DecodeId(id);
        var passkeys = await userManager.GetPasskeysAsync(user);
        if (!passkeys.Any(passkey => passkey.CredentialId.SequenceEqual(credentialId)))
        {
            throw new ItemNotFoundException("패스키를 찾지 못했어요.");
        }

        if (passkeys.Count == 1 && !await userManager.HasPasswordAsync(user) && (await userManager.GetLoginsAsync(user)).Count == 0)
        {
            throw new LastLoginMethodException("로그인할 방법이 이 패스키 하나뿐이라 지울 수 없어요. 비밀번호를 정하거나 다른 수단을 먼저 추가해 주세요.");
        }

        EnsureSucceeded(await userManager.RemovePasskeyAsync(user, credentialId));
        return TypedResults.NoContent();
    }

    private static async Task<ApplicationUser> CurrentUserAsync(HttpContext context, UserManager<ApplicationUser> userManager) =>
        await userManager.GetUserAsync(context.User) ?? throw new InvalidOperationException("세션의 사용자가 없다");

    private static string NameOf(string? name) =>
        string.IsNullOrWhiteSpace(name) ? "패스키" : name.Trim()[..Math.Min(name.Trim().Length, 50)];

    private static byte[] DecodeId(string id)
    {
        try
        {
            return WebEncoders.Base64UrlDecode(id);
        }
        catch (FormatException)
        {
            throw new ItemNotFoundException("패스키를 찾지 못했어요.");
        }
    }

    private static void EnsureSucceeded(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"패스키를 저장하지 못했다 — {string.Join(", ", result.Errors.Select(error => error.Code))}");
        }
    }

    private static PasskeyResponse ToResponse(UserPasskeyInfo passkey) =>
        new(WebEncoders.Base64UrlEncode(passkey.CredentialId), passkey.Name ?? "패스키", passkey.CreatedAt);

    public sealed record PasskeyCredentialRequest(string Credential);

    public sealed record AddPasskeyRequest(string Credential, string? Name);

    public sealed record RenamePasskeyRequest(string? Name);

    public sealed record PasskeyResponse(string Id, string Name, DateTimeOffset CreatedAt);
}
