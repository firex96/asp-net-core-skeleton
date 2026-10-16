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
/// 가입 · 이메일 확인 · 로그인 · 로그아웃 · 현재 사용자 · 비밀번호 재설정 — <c>/api/auth</c>. Identity 를 직접 다루는 프레임워크 구성이라 계층 규칙(Controller → MappingService …)
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
        auth.MapPost("/email/confirm", ConfirmEmailAsync).AllowAnonymous();
        auth.MapPost("/email/resend", ResendConfirmationAsync).AllowAnonymous();
        return endpoints;
    }

    /// <summary>계정을 만들고 이메일 확인 링크를 보낸다. 로그인하지 않는다 — 이메일을 확인해야 로그인할 수 있다.</summary>
    private static async Task<Results<Created, ValidationProblem>> RegisterAsync(
        CredentialsRequest request, UserManager<ApplicationUser> userManager, IEmailSender<ApplicationUser> emailSender, IOptions<AppOptions> app)
    {
        var user = new ApplicationUser { UserName = request.Email, Email = request.Email };
        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return TypedResults.ValidationProblem(result.Errors
                .GroupBy(error => error.Code)
                .ToDictionary(group => group.Key, group => group.Select(error => error.Description).ToArray()));
        }

        await SendConfirmationLinkAsync(user, userManager, emailSender, app.Value);
        return TypedResults.Created("/api/auth/me");
    }

    /// <summary>
    /// 틀린 이메일 · 비밀번호는 같은 401 이다(계정이 있는지 드러내지 않는다). 5번 틀리면 잠기고 423 이다.
    /// 이메일을 확인하지 않은 계정은 비밀번호가 맞을 때만 403 <c>EMAIL_NOT_CONFIRMED_ERROR</c> 다 — Identity 는 비밀번호를 보기 전에
    /// 확인 여부로 거절하므로(계정 존재가 드러나고 실패 횟수도 세지 않는다) 그 경우만 여기서 먼저 판정한다.
    /// </summary>
    private static async Task<Results<NoContent, ProblemHttpResult>> LoginAsync(
        CredentialsRequest request, SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is { EmailConfirmed: false })
        {
            return await CheckUnconfirmedAsync(user, request.Password, userManager);
        }

        var result = await signInManager.PasswordSignInAsync(request.Email, request.Password, isPersistent: false, lockoutOnFailure: true);
        if (result.Succeeded)
        {
            return TypedResults.NoContent();
        }

        return result.IsLockedOut ? Locked() : WrongCredentials();
    }

    private static async Task<ProblemHttpResult> CheckUnconfirmedAsync(ApplicationUser user, string password, UserManager<ApplicationUser> userManager)
    {
        if (await userManager.IsLockedOutAsync(user))
        {
            return Locked();
        }

        if (!await userManager.CheckPasswordAsync(user, password))
        {
            await userManager.AccessFailedAsync(user);
            return await userManager.IsLockedOutAsync(user) ? Locked() : WrongCredentials();
        }

        await userManager.ResetAccessFailedCountAsync(user);
        throw new EmailNotConfirmedException("이메일 확인을 아직 마치지 않았어요. 가입할 때 받은 메일의 링크를 열어 주세요.");
    }

    private static ProblemHttpResult Locked() =>
        TypedResults.Problem(statusCode: StatusCodes.Status423Locked, detail: "로그인에 여러 번 실패해서 계정이 잠겼어요. 30분 뒤에 다시 시도하거나 비밀번호를 재설정해 주세요.");

    private static ProblemHttpResult WrongCredentials() =>
        TypedResults.Problem(statusCode: StatusCodes.Status401Unauthorized, detail: "이메일 또는 비밀번호가 맞지 않아요.");

    /// <summary>
    /// 메일 링크(<c>/confirm-email?userId=…&amp;code=…</c>)의 토큰으로 이메일을 확인한다. 틀렸거나 만료됐으면 400 <c>INVALID_TOKEN_ERROR</c> 다.
    /// 로그인시키지 않는다 — 링크만 가진 사람이 로그인되지 않게 한다.
    /// </summary>
    private static async Task<NoContent> ConfirmEmailAsync(ConfirmEmailRequest request, UserManager<ApplicationUser> userManager)
    {
        var user = await userManager.FindByIdAsync(request.UserId.ToString(System.Globalization.CultureInfo.InvariantCulture))
                   ?? throw InvalidConfirmationToken();
        if (!TryDecode(request.Code, out var token))
        {
            throw InvalidConfirmationToken();
        }

        var result = await userManager.ConfirmEmailAsync(user, token);
        return result.Succeeded ? TypedResults.NoContent() : throw InvalidConfirmationToken();
    }

    /// <summary>확인 메일을 다시 보낸다. 계정이 없거나 이미 확인했어도 202 다 — 응답으로 가입 여부를 알 수 없게 한다.</summary>
    private static async Task<Accepted> ResendConfirmationAsync(
        ResendConfirmationRequest request, UserManager<ApplicationUser> userManager, IEmailSender<ApplicationUser> emailSender, IOptions<AppOptions> app)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is { EmailConfirmed: false })
        {
            await SendConfirmationLinkAsync(user, userManager, emailSender, app.Value);
        }

        return TypedResults.Accepted((string?)null);
    }

    private static async Task SendConfirmationLinkAsync(
        ApplicationUser user, UserManager<ApplicationUser> userManager, IEmailSender<ApplicationUser> emailSender, AppOptions app)
    {
        var code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(await userManager.GenerateEmailConfirmationTokenAsync(user)));
        var link = $"{app.PublicOrigin.TrimEnd('/')}/confirm-email?userId={user.Id}&code={code}";
        await emailSender.SendConfirmationLinkAsync(user, user.Email!, link);
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
        if (!TryDecode(request.Code, out var token))
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
        new("재설정 링크가 맞지 않거나 이미 만료됐어요. 비밀번호 찾기를 다시 해 주세요.");

    private static InvalidTokenException InvalidConfirmationToken() =>
        new("확인 링크가 맞지 않거나 이미 만료됐어요. 로그인 화면에서 확인 메일을 다시 받아 주세요.");

    /// <summary>메일 링크의 <c>code</c>(base64url) → Identity 토큰.</summary>
    private static bool TryDecode(string code, out string token)
    {
        try
        {
            token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
            return true;
        }
        catch (FormatException)
        {
            token = string.Empty;
            return false;
        }
    }

    public sealed record CredentialsRequest(string Email, string Password);

    public sealed record ForgotPasswordRequest(string Email);

    public sealed record ResetPasswordRequest(string Email, string Code, string NewPassword);

    public sealed record ConfirmEmailRequest(long UserId, string Code);

    public sealed record ResendConfirmationRequest(string Email);

    public sealed record CurrentUserResponse(long Id, string Email, IReadOnlyList<string> Permissions);
}
