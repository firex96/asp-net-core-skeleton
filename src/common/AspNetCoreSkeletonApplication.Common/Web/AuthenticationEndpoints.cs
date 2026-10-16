using System.Security.Claims;
using System.Text;
using AspNetCoreSkeletonApplication.Common.Authorization.Services.Biz;
using AspNetCoreSkeletonApplication.Common.Errors;
using AspNetCoreSkeletonApplication.Common.Persistence.Identity;
using AspNetCoreSkeletonApplication.Common.Web.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace AspNetCoreSkeletonApplication.Common.Web;

/// <summary>
/// 가입 · 로그인 · 로그아웃 · 현재 사용자 · 비밀번호 재설정 — <c>/api/auth</c>. Identity 를 직접 다루는 프레임워크 구성이라 계층 규칙(Controller → MappingService …)
/// 밖에 둔다(GlobalExceptionHandler 와 같은 자리). <c>MapIdentityApi</c> 는 쓰지 않는다 — Bearer 토큰 · 메일 흐름이 딸려 온다.
/// </summary>
public static class AuthenticationEndpoints
{
    public static IEndpointRouteBuilder MapSkeletonAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var auth = endpoints.MapGroup("/api/auth");
        auth.MapPost("/register", RegisterAsync).AllowAnonymous();
        auth.MapPost("/login", LoginAsync).AllowAnonymous();
        auth.MapPost("/logout", LogoutAsync).RequireAuthorization();
        auth.MapGet("/me", MeAsync).RequireAuthorization();
        auth.MapPost("/password/forgot", ForgotPasswordAsync).AllowAnonymous();
        auth.MapPost("/password/reset", ResetPasswordAsync).AllowAnonymous();
        return endpoints;
    }

    private static async Task<Results<Created, ValidationProblem>> RegisterAsync(
        CredentialsRequest request, UserManager<ApplicationUser> userManager)
    {
        var user = new ApplicationUser { UserName = request.Email, Email = request.Email };
        var result = await userManager.CreateAsync(user, request.Password);
        return result.Succeeded
            ? TypedResults.Created("/api/auth/me")
            : TypedResults.ValidationProblem(result.Errors
                .GroupBy(error => error.Code)
                .ToDictionary(group => group.Key, group => group.Select(error => error.Description).ToArray()));
    }

    /// <summary>틀린 이메일 · 비밀번호는 같은 401 이다(계정이 있는지 드러내지 않는다). 5번 틀리면 잠기고 423 이다.</summary>
    private static async Task<Results<NoContent, ProblemHttpResult>> LoginAsync(
        CredentialsRequest request, SignInManager<ApplicationUser> signInManager)
    {
        var result = await signInManager.PasswordSignInAsync(request.Email, request.Password, isPersistent: false, lockoutOnFailure: true);
        if (result.Succeeded)
        {
            return TypedResults.NoContent();
        }

        return result.IsLockedOut
            ? TypedResults.Problem(statusCode: StatusCodes.Status423Locked, detail: "로그인을 여러 번 실패해 계정이 잠겼다. 잠시 뒤에 다시 시도한다")
            : TypedResults.Problem(statusCode: StatusCodes.Status401Unauthorized, detail: "이메일 또는 비밀번호가 맞지 않는다");
    }

    private static async Task<NoContent> LogoutAsync(SignInManager<ApplicationUser> signInManager)
    {
        await signInManager.SignOutAsync();
        return TypedResults.NoContent();
    }

    /// <summary>현재 사용자와 그 실효 권한 — SPA 가 화면에서 쓸 수 있는 기능을 고를 때 쓴다(서버는 요청마다 다시 판정한다).</summary>
    /// 사용자가 없는 클라이언트 토큰이면 403 이다. 토큰이면 권한은 토큰 scope 와의 교집합이다.
    private static async Task<Results<Ok<CurrentUserResponse>, ForbidHttpResult>> MeAsync(
        ClaimsPrincipal user, RequestPermissions requestPermissions, CancellationToken cancellationToken)
    {
        if (UserClaims.IdOf(user) is not { } id)
        {
            return TypedResults.Forbid();
        }

        var permissions = await requestPermissions.ListOfUserAsync(user, id, cancellationToken);
        return TypedResults.Ok(new CurrentUserResponse(
            id,
            user.FindFirstValue(ClaimTypes.Email) ?? user.FindFirstValue("email") ?? user.Identity!.Name!,
            permissions.Order(StringComparer.Ordinal).ToArray()));
    }

    /// <summary>
    /// 재설정 링크를 메일로 보낸다. 계정이 있든 없든 202 다 — 응답으로 가입 여부를 알 수 없게 한다. 링크는 <c>App:PublicOrigin</c> 의
    /// SPA 화면(<c>/reset-password?email=…&amp;code=…</c>)을 가리키고, 토큰은 2시간 동안 한 번만 쓸 수 있다.
    /// </summary>
    private static async Task<Accepted> ForgotPasswordAsync(
        ForgotPasswordRequest request,
        UserManager<ApplicationUser> userManager,
        IEmailSender<ApplicationUser> emailSender,
        IOptions<AppOptions> app)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is not null)
        {
            var code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(await userManager.GeneratePasswordResetTokenAsync(user)));
            var link = $"{app.Value.PublicOrigin.TrimEnd('/')}/reset-password?email={Uri.EscapeDataString(user.Email!)}&code={code}";
            await emailSender.SendPasswordResetLinkAsync(user, user.Email!, link);
        }

        return TypedResults.Accepted((string?)null);
    }

    /// <summary>
    /// 새 비밀번호를 정한다. 토큰이 틀렸거나 만료됐거나 이미 쓰였으면 400 <c>INVALID_TOKEN_ERROR</c>, 비밀번호 규칙을 어기면 400 <c>NOT_VALID_ERROR</c>.
    /// 성공하면 보안 스탬프가 바뀌어 같은 토큰과 다른 기기의 세션이 무효가 되고(세션은 다음 검증 때), 로그인 잠금도 풀린다.
    /// </summary>
    private static async Task<Results<NoContent, ValidationProblem>> ResetPasswordAsync(
        ResetPasswordRequest request, UserManager<ApplicationUser> userManager)
    {
        var user = await userManager.FindByEmailAsync(request.Email) ?? throw InvalidResetToken();
        string token;
        try
        {
            token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(request.Code));
        }
        catch (FormatException)
        {
            throw InvalidResetToken();
        }

        var result = await userManager.ResetPasswordAsync(user, token, request.NewPassword);
        if (result.Errors.Any(error => error.Code == nameof(IdentityErrorDescriber.InvalidToken)))
        {
            throw InvalidResetToken();
        }

        if (!result.Succeeded)
        {
            return TypedResults.ValidationProblem(result.Errors
                .GroupBy(error => error.Code)
                .ToDictionary(group => group.Key, group => group.Select(error => error.Description).ToArray()));
        }

        await userManager.SetLockoutEndDateAsync(user, null);
        await userManager.ResetAccessFailedCountAsync(user);
        return TypedResults.NoContent();
    }

    private static InvalidTokenException InvalidResetToken() =>
        new("재설정 링크가 맞지 않거나 만료됐다. 비밀번호 찾기를 다시 요청한다");

    public sealed record CredentialsRequest(string Email, string Password);

    public sealed record ForgotPasswordRequest(string Email);

    public sealed record ResetPasswordRequest(string Email, string Code, string NewPassword);

    public sealed record CurrentUserResponse(long Id, string Email, IReadOnlyList<string> Permissions);
}
