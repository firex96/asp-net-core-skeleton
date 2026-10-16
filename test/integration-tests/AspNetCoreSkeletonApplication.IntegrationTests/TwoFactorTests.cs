using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AspNetCoreSkeletonApplication.IntegrationTests.Support;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AspNetCoreSkeletonApplication.IntegrationTests;

/// <summary>
/// 2단계 인증을 끝까지 — 인증 앱(TOTP) 등록 · 로그인, 메일 코드(Mailpit), 복구 코드 1회용, 브라우저 기억, 끄기(비밀번호 재확인).
/// 쿠키는 실제 쿠키 상자로 주고받는다(두 번째 단계를 기다리는 사용자는 <c>__Host-tfa</c> 쿠키에 있다).
/// </summary>
[Collection(InfrastructureCollection.Name)]
public class TwoFactorTests(InfrastructureFixture infrastructure) : IAsyncLifetime
{
    private const string Database = "two_factor";

    private WebApplicationFactory<Program> _factory = null!;
    private readonly HttpClient _mailpit = new() { BaseAddress = infrastructure.MailpitApi };

    public async ValueTask InitializeAsync()
    {
        await infrastructure.RecreateDatabaseAsync(Database);
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => infrastructure.ConfigureApp(builder, Database));
    }

    public async ValueTask DisposeAsync()
    {
        _mailpit.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public void Totp_MatchesTheRfc6238TestVector()
    {
        // RFC 6238 부록 B — 키 "12345678901234567890", T=59초 → 94287082 의 끝 6자리
        Assert.Equal("287082", Totp.Code("GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ", DateTimeOffset.FromUnixTimeSeconds(59)));
    }

    [Fact]
    public async Task Authenticator_IsRequiredAfterThePassword()
    {
        const string email = "authenticator@example.com";
        var (owner, _) = await TestUsers.SignInAsync(_factory, email, administrator: false);
        var key = await RegisterAuthenticatorAsync(owner);
        Assert.Equal("authenticator", (await StatusAsync(owner)).GetProperty("method").GetString());

        var browser = NewBrowser();
        var login = await LoginAsync(browser, email);
        Assert.Equal(HttpStatusCode.Accepted, login.StatusCode);
        Assert.Equal(["authenticator", "recovery"], Providers(await ReadJsonAsync(login)));
        Assert.Equal(HttpStatusCode.Unauthorized, (await browser.GetAsync("/api/auth/me", Token)).StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized, (await SecondStepAsync(browser, "authenticator", "000000")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await SecondStepAsync(browser, "email", "123456")).StatusCode); // 인증 앱 계정은 메일 코드를 받지 않는다
        Assert.Equal(HttpStatusCode.NoContent, (await SecondStepAsync(browser, "authenticator", Totp.Code(key))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await browser.GetAsync("/api/auth/me", Token)).StatusCode);
    }

    [Fact]
    public async Task EmailCode_IsMailedForTheSecondStep()
    {
        const string email = "email-code@example.com";
        var (owner, _) = await TestUsers.SignInAsync(_factory, email, administrator: false);
        var codes = await ReadJsonAsync(await owner.PostAsync("/api/account/two-factor/email/enable", null, Token));
        Assert.Equal(10, codes.GetProperty("recoveryCodes").GetArrayLength());

        var browser = NewBrowser();
        Assert.Equal(["email", "recovery"], Providers(await ReadJsonAsync(await LoginAsync(browser, email))));
        Assert.Equal(HttpStatusCode.Accepted, (await browser.PostAsync("/api/auth/login/2fa/email", null, Token)).StatusCode);
        var code = await ReadSignInCodeAsync(email);

        Assert.Equal(HttpStatusCode.NoContent, (await SecondStepAsync(browser, "email", code)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await browser.GetAsync("/api/auth/me", Token)).StatusCode);
    }

    [Fact]
    public async Task RecoveryCode_WorksOnce()
    {
        const string email = "recovery@example.com";
        var (owner, _) = await TestUsers.SignInAsync(_factory, email, administrator: false);
        await RegisterAuthenticatorAsync(owner);
        var recovery = (await ReadJsonAsync(await owner.PostAsync("/api/account/two-factor/recovery-codes", null, Token)))
            .GetProperty("recoveryCodes")[0].GetString()!;

        var first = NewBrowser();
        await LoginAsync(first, email);
        Assert.Equal(HttpStatusCode.NoContent, (await SecondStepAsync(first, "recovery", recovery)).StatusCode);

        var second = NewBrowser();
        await LoginAsync(second, email);
        Assert.Equal(HttpStatusCode.Unauthorized, (await SecondStepAsync(second, "recovery", recovery)).StatusCode);
        Assert.Equal(9, (await StatusAsync(owner)).GetProperty("recoveryCodesLeft").GetInt32());
    }

    [Fact]
    public async Task RememberedBrowser_SkipsTheSecondStep_UntilForgotten()
    {
        const string email = "remember@example.com";
        var (owner, _) = await TestUsers.SignInAsync(_factory, email, administrator: false);
        var key = await RegisterAuthenticatorAsync(owner);

        var browser = NewBrowser();
        await LoginAsync(browser, email);
        Assert.Equal(HttpStatusCode.NoContent, (await SecondStepAsync(browser, "authenticator", Totp.Code(key), rememberBrowser: true)).StatusCode);
        Assert.True((await StatusAsync(browser)).GetProperty("browserRemembered").GetBoolean());
        await browser.PostAsync("/api/auth/logout", null, Token);

        Assert.Equal(HttpStatusCode.NoContent, (await LoginAsync(browser, email)).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await browser.PostAsync("/api/account/two-factor/forget-browser", null, Token)).StatusCode);
        await browser.PostAsync("/api/auth/logout", null, Token);
        Assert.Equal(HttpStatusCode.Accepted, (await LoginAsync(browser, email)).StatusCode);
    }

    [Fact]
    public async Task Disable_NeedsThePassword_AndEnablingTwiceConflicts()
    {
        const string email = "disable@example.com";
        var (owner, _) = await TestUsers.SignInAsync(_factory, email, administrator: false);
        await RegisterAuthenticatorAsync(owner);

        Assert.Equal(HttpStatusCode.Conflict, (await owner.PostAsync("/api/account/two-factor/email/enable", null, Token)).StatusCode);
        var wrong = await owner.PostAsJsonAsync("/api/account/two-factor/disable", new { password = "Wr0ng-Passw0rd!" }, Token);
        Assert.Equal(HttpStatusCode.BadRequest, wrong.StatusCode);
        var right = await owner.PostAsJsonAsync("/api/account/two-factor/disable", new { password = TestUsers.Password }, Token);
        Assert.Equal(HttpStatusCode.NoContent, right.StatusCode);

        var status = await StatusAsync(owner);
        Assert.False(status.GetProperty("enabled").GetBoolean());
        Assert.Equal(HttpStatusCode.NoContent, (await LoginAsync(NewBrowser(), email)).StatusCode);
    }

    [Fact]
    public async Task SecondStep_WithoutAPendingSignIn_Is401()
    {
        var browser = NewBrowser();

        Assert.Equal(HttpStatusCode.Unauthorized, (await browser.GetAsync("/api/auth/login/2fa", Token)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await SecondStepAsync(browser, "authenticator", "123456")).StatusCode);
    }

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private HttpClient NewBrowser() => _factory.CreateClient(TestUsers.Https);

    private static Task<HttpResponseMessage> LoginAsync(HttpClient browser, string email) =>
        browser.PostAsJsonAsync("/api/auth/login", new { email, password = TestUsers.Password }, Token);

    private static Task<HttpResponseMessage> SecondStepAsync(HttpClient browser, string provider, string code, bool rememberBrowser = false) =>
        browser.PostAsJsonAsync("/api/auth/login/2fa", new { provider, code, rememberBrowser }, Token);

    /// <summary>인증 앱을 등록하고(공유 키 → 지금 코드로 확인) 공유 키를 돌려준다.</summary>
    private static async Task<string> RegisterAuthenticatorAsync(HttpClient owner)
    {
        var setup = await ReadJsonAsync(await owner.PostAsync("/api/account/two-factor/authenticator/setup", null, Token));
        Assert.StartsWith("data:image/svg+xml;base64,", setup.GetProperty("qrCodeDataUri").GetString(), StringComparison.Ordinal);
        Assert.StartsWith("otpauth://totp/", setup.GetProperty("authenticatorUri").GetString(), StringComparison.Ordinal);
        var key = setup.GetProperty("sharedKey").GetString()!;

        var wrong = await owner.PostAsJsonAsync("/api/account/two-factor/authenticator/verify", new { code = "000000" }, Token);
        Assert.Equal(HttpStatusCode.BadRequest, wrong.StatusCode);
        var verified = await owner.PostAsJsonAsync("/api/account/two-factor/authenticator/verify", new { code = Totp.Code(key) }, Token);
        Assert.Equal(HttpStatusCode.OK, verified.StatusCode);
        Assert.Equal(10, (await ReadJsonAsync(verified)).GetProperty("recoveryCodes").GetArrayLength());
        return key;
    }

    private static async Task<JsonElement> StatusAsync(HttpClient client) =>
        await ReadJsonAsync(await client.GetAsync("/api/account/two-factor", Token));

    private static string[] Providers(JsonElement challenge) =>
        [.. challenge.GetProperty("providers").EnumerateArray().Select(provider => provider.GetString()!)];

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>(Token);

    /// <summary>로그인 확인 코드 메일의 6자리 코드. 아웃박스를 거쳐 오므로 올 때까지 기다린다.</summary>
    private async Task<string> ReadSignInCodeAsync(string email)
    {
        var query = Uri.EscapeDataString($"to:\"{email}\" subject:\"로그인 확인 코드\"");
        for (var attempt = 0; attempt < 50; attempt++)
        {
            var search = await _mailpit.GetFromJsonAsync<JsonElement>($"api/v1/search?query={query}", Token);
            if (search.GetProperty("messages").GetArrayLength() > 0)
            {
                var id = search.GetProperty("messages")[0].GetProperty("ID").GetString();
                var message = await _mailpit.GetFromJsonAsync<JsonElement>($"api/v1/message/{id}", Token);
                return System.Text.RegularExpressions.Regex.Match(message.GetProperty("Text").GetString()!, @"\b\d{6}\b").Value;
            }

            await Task.Delay(200, Token);
        }

        throw new TimeoutException($"{email} 에게 로그인 확인 코드가 오지 않았다");
    }
}
