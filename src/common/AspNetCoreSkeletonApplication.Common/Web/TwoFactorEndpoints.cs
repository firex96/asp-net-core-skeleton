using System.Text;
using System.Text.Encodings.Web;
using AspNetCoreSkeletonApplication.Common.Errors;
using AspNetCoreSkeletonApplication.Common.Mail;
using AspNetCoreSkeletonApplication.Common.Persistence.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using QRCoder;

namespace AspNetCoreSkeletonApplication.Common.Web;

/// <summary>
/// 2단계 인증 — 로그인의 두 번째 단계(<c>/api/auth/login/2fa</c>)와 내 계정의 2단계 인증 관리(<c>/api/account/two-factor</c>). Identity 의 기본 기능
/// (인증 앱 TOTP · 메일 코드 · 복구 코드 · 브라우저 기억)을 그대로 쓴다.
/// <list type="bullet">
/// <item>수단은 하나다 — 인증 앱을 등록했으면 인증 앱, 아니면 메일 코드. 복구 코드는 언제나 함께 쓸 수 있다. 인증 앱을 고른 사람에게 메일 코드를 열어 두면
/// 메일 계정 하나로 두 단계를 다 넘게 된다.</item>
/// <item>켜기(인증 앱 등록 · 메일 코드)는 꺼져 있을 때만 한다 — 등록하다 만 키가 로그인 수단이 되지 않는다. 켤 때마다 복구 코드 10개를 새로 만들어 한 번만 보여 준다.</item>
/// <item>끄기는 비밀번호를 다시 확인한다. 관리 API 는 세션 쿠키로만 부른다(Bearer 토큰으로는 계정 보안을 바꾸지 못한다).</item>
/// </list>
/// </summary>
public static class TwoFactorEndpoints
{
    public const string Authenticator = "authenticator";
    public const string Email = "email";
    public const string Recovery = "recovery";

    private const string Issuer = "AspNetCoreSkeletonApplication";
    private const int RecoveryCodeCount = 10;

    // UserStore 가 인증 앱 키를 두는 자리 — Identity 의 내부 이름이다(UserStoreBase).
    private const string InternalLoginProvider = "[AspNetUserStore]";
    private const string AuthenticatorKeyTokenName = "AuthenticatorKey";

    public static IEndpointRouteBuilder MapSkeletonTwoFactorEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var login = endpoints.MapGroup("/api/auth/login/2fa").AllowAnonymous();
        login.MapGet("", PendingAsync);
        login.MapPost("", SignInAsync);
        login.MapPost("/email", SendEmailCodeAsync);

        var sessionOnly = new AuthorizationPolicyBuilder(IdentityConstants.ApplicationScheme).RequireAuthenticatedUser().Build();
        var account = endpoints.MapGroup("/api/account/two-factor").RequireAuthorization(sessionOnly);
        account.MapGet("", StatusAsync);
        account.MapPost("/authenticator/setup", SetupAuthenticatorAsync);
        account.MapPost("/authenticator/verify", VerifyAuthenticatorAsync);
        account.MapPost("/email/enable", EnableEmailAsync);
        account.MapPost("/recovery-codes", RegenerateRecoveryCodesAsync);
        account.MapPost("/disable", DisableAsync);
        account.MapPost("/forget-browser", ForgetBrowserAsync);
        return endpoints;
    }

    /// <summary>로그인 두 번째 단계에서 쓸 수 있는 수단(<c>authenticator</c> 또는 <c>email</c>, 남은 복구 코드가 있으면 <c>recovery</c>).</summary>
    internal static async Task<IReadOnlyList<string>> MethodsAsync(UserManager<ApplicationUser> userManager, ApplicationUser user)
    {
        List<string> methods = [await userManager.GetAuthenticatorKeyAsync(user) is null ? Email : Authenticator];
        if (await userManager.CountRecoveryCodesAsync(user) > 0)
        {
            methods.Add(Recovery);
        }

        return methods;
    }

    /// <summary>비밀번호(또는 소셜 로그인)를 마치고 두 번째 단계를 기다리는 중이면 그 수단들. 아니면 401 이다.</summary>
    private static async Task<Results<Ok<TwoFactorChallengeResponse>, ProblemHttpResult>> PendingAsync(
        SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager)
    {
        var user = await signInManager.GetTwoFactorAuthenticationUserAsync();
        return user is null ? NoPendingSignIn() : TypedResults.Ok(new TwoFactorChallengeResponse(await MethodsAsync(userManager, user)));
    }

    /// <summary>두 번째 단계 — 맞으면 204 + 세션 쿠키. 틀리면 401 이고 잠금 횟수를 센다(5번이면 423).</summary>
    private static async Task<Results<NoContent, ProblemHttpResult>> SignInAsync(
        TwoFactorSignInRequest request, SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager)
    {
        var user = await signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user is null)
        {
            return NoPendingSignIn();
        }

        if (!(await MethodsAsync(userManager, user)).Contains(request.Provider))
        {
            return WrongCode();
        }

        var code = request.Code.Replace(" ", string.Empty, StringComparison.Ordinal);
        var result = request.Provider switch
        {
            Authenticator => await signInManager.TwoFactorAuthenticatorSignInAsync(code, isPersistent: false, request.RememberBrowser),
            Email => await signInManager.TwoFactorSignInAsync(TokenOptions.DefaultEmailProvider, code, isPersistent: false, request.RememberBrowser),
            _ => await signInManager.TwoFactorRecoveryCodeSignInAsync(code),
        };
        if (result.Succeeded)
        {
            return TypedResults.NoContent();
        }

        return result.IsLockedOut
            ? TypedResults.Problem(statusCode: StatusCodes.Status423Locked, detail: "로그인에 여러 번 실패해서 계정이 잠겼어요. 30분 뒤에 다시 시도하거나 비밀번호를 재설정해 주세요.")
            : WrongCode();
    }

    /// <summary>메일로 로그인 확인 코드를 보낸다(아웃박스). 메일 코드를 쓰는 계정만 — 인증 앱을 등록한 계정은 400 이다.</summary>
    private static async Task<Results<Accepted, ProblemHttpResult>> SendEmailCodeAsync(
        SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, IdentityEmailSender emailSender)
    {
        var user = await signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user is null)
        {
            return NoPendingSignIn();
        }

        if (!(await MethodsAsync(userManager, user)).Contains(Email))
        {
            throw new ClientBadRequestException("이 계정은 인증 앱으로 확인해요.");
        }

        var code = await userManager.GenerateTwoFactorTokenAsync(user, TokenOptions.DefaultEmailProvider);
        await emailSender.SendSignInCodeAsync(user.Email!, code);
        return TypedResults.Accepted((string?)null);
    }

    private static async Task<Ok<TwoFactorStatusResponse>> StatusAsync(
        HttpContext context, SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager)
    {
        var user = await CurrentUserAsync(context, userManager);
        var enabled = await userManager.GetTwoFactorEnabledAsync(user);
        return TypedResults.Ok(new TwoFactorStatusResponse(
            enabled,
            enabled ? (await MethodsAsync(userManager, user))[0] : null,
            enabled ? await userManager.CountRecoveryCodesAsync(user) : 0,
            await signInManager.IsTwoFactorClientRememberedAsync(user)));
    }

    /// <summary>인증 앱 등록을 시작한다 — 새 공유 키와 그 QR 코드. 아직 켜지 않는다(<see cref="VerifyAuthenticatorAsync"/> 가 켠다).</summary>
    private static async Task<Ok<AuthenticatorSetupResponse>> SetupAuthenticatorAsync(HttpContext context, UserManager<ApplicationUser> userManager)
    {
        var user = await CurrentUserAsync(context, userManager);
        await EnsureDisabledAsync(userManager, user);
        await userManager.ResetAuthenticatorKeyAsync(user);
        var key = (await userManager.GetAuthenticatorKeyAsync(user))!;
        var uri = $"otpauth://totp/{UrlEncoder.Default.Encode(Issuer)}:{UrlEncoder.Default.Encode(user.Email!)}?secret={key}&issuer={UrlEncoder.Default.Encode(Issuer)}&digits=6";
        return TypedResults.Ok(new AuthenticatorSetupResponse(FormatKey(key), uri, QrCodeDataUri(uri)));
    }

    /// <summary>인증 앱의 코드가 맞으면 2단계 인증을 켜고 복구 코드를 돌려준다(이때 한 번만 보여 준다).</summary>
    private static async Task<Ok<RecoveryCodesResponse>> VerifyAuthenticatorAsync(
        VerifyAuthenticatorRequest request, HttpContext context, UserManager<ApplicationUser> userManager)
    {
        var user = await CurrentUserAsync(context, userManager);
        await EnsureDisabledAsync(userManager, user);
        var code = request.Code.Replace(" ", string.Empty, StringComparison.Ordinal);
        if (await userManager.GetAuthenticatorKeyAsync(user) is null
            || !await userManager.VerifyTwoFactorTokenAsync(user, userManager.Options.Tokens.AuthenticatorTokenProvider, code))
        {
            throw new ClientBadRequestException("코드가 맞지 않아요. 인증 앱에 지금 보이는 6자리 코드를 넣어 주세요.");
        }

        return TypedResults.Ok(await EnableAsync(userManager, user));
    }

    /// <summary>메일 코드로 2단계 인증을 켠다. 등록하다 만 인증 앱 키는 지운다.</summary>
    private static async Task<Ok<RecoveryCodesResponse>> EnableEmailAsync(HttpContext context, UserManager<ApplicationUser> userManager)
    {
        var user = await CurrentUserAsync(context, userManager);
        await EnsureDisabledAsync(userManager, user);
        await userManager.RemoveAuthenticationTokenAsync(user, InternalLoginProvider, AuthenticatorKeyTokenName);
        return TypedResults.Ok(await EnableAsync(userManager, user));
    }

    /// <summary>복구 코드를 새로 만든다 — 전에 받은 코드는 더 쓰지 못한다.</summary>
    private static async Task<Ok<RecoveryCodesResponse>> RegenerateRecoveryCodesAsync(HttpContext context, UserManager<ApplicationUser> userManager)
    {
        var user = await CurrentUserAsync(context, userManager);
        if (!await userManager.GetTwoFactorEnabledAsync(user))
        {
            throw new ConflictException("2단계 인증을 켠 뒤에 복구 코드를 받을 수 있어요.");
        }

        return TypedResults.Ok(new RecoveryCodesResponse((await userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, RecoveryCodeCount))!.ToArray()));
    }

    /// <summary>비밀번호를 다시 확인하고 끈다. 인증 앱 키도 지운다 — 다시 켤 때 새로 등록한다.</summary>
    private static async Task<NoContent> DisableAsync(
        DisableTwoFactorRequest request, HttpContext context, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
    {
        var user = await CurrentUserAsync(context, userManager);
        if (!await userManager.CheckPasswordAsync(user, request.Password))
        {
            throw new ClientBadRequestException("비밀번호가 맞지 않아요.");
        }

        await userManager.SetTwoFactorEnabledAsync(user, false);
        await userManager.RemoveAuthenticationTokenAsync(user, InternalLoginProvider, AuthenticatorKeyTokenName);
        await signInManager.ForgetTwoFactorClientAsync();
        return TypedResults.NoContent();
    }

    private static async Task<NoContent> ForgetBrowserAsync(SignInManager<ApplicationUser> signInManager)
    {
        await signInManager.ForgetTwoFactorClientAsync();
        return TypedResults.NoContent();
    }

    private static async Task<RecoveryCodesResponse> EnableAsync(UserManager<ApplicationUser> userManager, ApplicationUser user)
    {
        await userManager.SetTwoFactorEnabledAsync(user, true);
        return new RecoveryCodesResponse((await userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, RecoveryCodeCount))!.ToArray());
    }

    private static async Task EnsureDisabledAsync(UserManager<ApplicationUser> userManager, ApplicationUser user)
    {
        if (await userManager.GetTwoFactorEnabledAsync(user))
        {
            throw new ConflictException("2단계 인증이 이미 켜져 있어요. 수단을 바꾸려면 끈 뒤에 다시 켜 주세요.");
        }
    }

    private static async Task<ApplicationUser> CurrentUserAsync(HttpContext context, UserManager<ApplicationUser> userManager) =>
        await userManager.GetUserAsync(context.User) ?? throw new InvalidOperationException("세션의 사용자가 없다");

    /// <summary><c>abcdefgh…</c> → <c>abcd efgh …</c> — 손으로 옮겨 적기 쉽게.</summary>
    private static string FormatKey(string key)
    {
        var formatted = new StringBuilder();
        for (var index = 0; index < key.Length; index += 4)
        {
            formatted.Append(key.AsSpan(index, Math.Min(4, key.Length - index))).Append(' ');
        }

        return formatted.ToString().TrimEnd().ToLowerInvariant();
    }

    /// <summary>QR 코드를 서버에서 SVG 로 그려 data URI 로 보낸다 — SPA 는 <c>&lt;img&gt;</c> 로만 그린다(QR 라이브러리를 SPA 마다 두지 않는다).</summary>
    private static string QrCodeDataUri(string text)
    {
        using var data = QRCodeGenerator.GenerateQrCode(text, QRCodeGenerator.ECCLevel.Q);
        var svg = new SvgQRCode(data).GetGraphic(4);
        return "data:image/svg+xml;base64," + Convert.ToBase64String(Encoding.UTF8.GetBytes(svg));
    }

    private static ProblemHttpResult NoPendingSignIn() =>
        TypedResults.Problem(statusCode: StatusCodes.Status401Unauthorized, detail: "로그인 시간이 지났어요. 처음부터 다시 로그인해 주세요.");

    private static ProblemHttpResult WrongCode() =>
        TypedResults.Problem(statusCode: StatusCodes.Status401Unauthorized, detail: "코드가 맞지 않아요. 다시 확인해 주세요.");

    public sealed record TwoFactorChallengeResponse(IReadOnlyList<string> Providers);

    public sealed record TwoFactorSignInRequest(string Provider, string Code, bool RememberBrowser);

    public sealed record TwoFactorStatusResponse(bool Enabled, string? Method, int RecoveryCodesLeft, bool BrowserRemembered);

    public sealed record AuthenticatorSetupResponse(string SharedKey, string AuthenticatorUri, string QrCodeDataUri);

    public sealed record VerifyAuthenticatorRequest(string Code);

    public sealed record RecoveryCodesResponse(IReadOnlyList<string> RecoveryCodes);

    public sealed record DisableTwoFactorRequest(string Password);
}
