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
        Assert.Equal(HttpStatusCode.NoContent, (await SecondStepAsync(browser, "authenticator", Totp.NextCode(key))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await browser.GetAsync("/api/auth/me", Token)).StatusCode);
    }

    [Fact]
    public async Task AuthenticatorCode_WorksOnce()
    {
        const string email = "authenticator-once@example.com";
        var (owner, _) = await TestUsers.SignInAsync(_factory, email, administrator: false);
        var key = await RegisterAuthenticatorAsync(owner);
        var code = Totp.NextCode(key);

        var first = NewBrowser();
        await LoginAsync(first, email);
        Assert.Equal(HttpStatusCode.NoContent, (await SecondStepAsync(first, "authenticator", code)).StatusCode);

        var second = NewBrowser();
        await LoginAsync(second, email);
        Assert.Equal(HttpStatusCode.Unauthorized, (await SecondStepAsync(second, "authenticator", code)).StatusCode);
    }

    [Fact]
    public async Task EarlierCode_AfterALaterOne_IsRefused()
    {
        const string email = "authenticator-earlier@example.com";
        var (owner, _) = await TestUsers.SignInAsync(_factory, email, administrator: false);
        var key = await RegisterAuthenticatorAsync(owner);
        var earlier = Totp.NextCode(key);
        var later = Totp.Code(key, DateTimeOffset.UtcNow.AddSeconds(60));

        var first = NewBrowser();
        await LoginAsync(first, email);
        Assert.Equal(HttpStatusCode.NoContent, (await SecondStepAsync(first, "authenticator", later)).StatusCode);

        // 아직 허용 범위 안이고 쓰인 적도 없지만, 통과한 단계보다 이르다
        var second = NewBrowser();
        await LoginAsync(second, email);
        Assert.Equal(HttpStatusCode.Unauthorized, (await SecondStepAsync(second, "authenticator", earlier)).StatusCode);
    }

    [Fact]
    public async Task SameCode_AtTheSameTime_SignsInOnce()
    {
        const string email = "authenticator-race@example.com";
        var (owner, _) = await TestUsers.SignInAsync(_factory, email, administrator: false);
        var key = await RegisterAuthenticatorAsync(owner);
        var browsers = Enumerable.Range(0, 8).Select(_ => NewBrowser()).ToArray();
        foreach (var browser in browsers)
        {
            await LoginAsync(browser, email);
        }

        var code = Totp.NextCode(key);
        var results = await Task.WhenAll(browsers.Select(browser => SecondStepAsync(browser, "authenticator", code)));

        AssertSignedInOnce(results);
    }

    [Fact]
    public async Task SameEmailCode_AtTheSameTime_SignsInOnce()
    {
        const string email = "email-race@example.com";
        var (owner, _) = await TestUsers.SignInAsync(_factory, email, administrator: false);
        await owner.PostAsync("/api/account/two-factor/email/enable", null, Token);
        var browsers = Enumerable.Range(0, 8).Select(_ => NewBrowser()).ToArray();
        foreach (var browser in browsers)
        {
            await LoginAsync(browser, email);
        }

        await browsers[0].PostAsync("/api/auth/login/2fa/email", null, Token);
        var code = await ReadSignInCodeAsync(email);
        var results = await Task.WhenAll(browsers.Select(browser => SecondStepAsync(browser, "email", code)));

        AssertSignedInOnce(results);
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
    public async Task EmailCode_WorksOnce()
    {
        const string email = "email-once@example.com";
        var (owner, _) = await TestUsers.SignInAsync(_factory, email, administrator: false);
        await owner.PostAsync("/api/account/two-factor/email/enable", null, Token);

        var first = NewBrowser();
        await LoginAsync(first, email);
        await first.PostAsync("/api/auth/login/2fa/email", null, Token);
        var code = await ReadSignInCodeAsync(email);
        Assert.Equal(HttpStatusCode.NoContent, (await SecondStepAsync(first, "email", code)).StatusCode);

        // 새 코드를 받지 않고 받은 코드를 다시 낸다 — 아직 시간 안이지만 쓰인 코드다
        var second = NewBrowser();
        await LoginAsync(second, email);
        Assert.Equal(HttpStatusCode.Unauthorized, (await SecondStepAsync(second, "email", code)).StatusCode);

        // 곧바로 다시 받은 코드는 새 코드라 통과한다
        await second.PostAsync("/api/auth/login/2fa/email", null, Token);
        var fresh = await ReadSignInCodeAsync(email, count: 2);
        Assert.NotEqual(code, fresh);
        Assert.Equal(HttpStatusCode.NoContent, (await SecondStepAsync(second, "email", fresh)).StatusCode);
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
        Assert.Equal(HttpStatusCode.NoContent, (await SecondStepAsync(browser, "authenticator", Totp.NextCode(key), rememberBrowser: true)).StatusCode);
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

    /// <summary>하나만 들어가고, 나머지는 틀린 코드(401) — 그사이 잠겼으면 423 이다. 서버 오류로 끝나지 않는다.</summary>
    private static void AssertSignedInOnce(HttpResponseMessage[] results)
    {
        Assert.Single(results, result => result.StatusCode == HttpStatusCode.NoContent);
        Assert.All(results, result => Assert.Contains(result.StatusCode, new[] { HttpStatusCode.NoContent, HttpStatusCode.Unauthorized, HttpStatusCode.Locked }));
    }

    private static async Task<JsonElement> StatusAsync(HttpClient client) =>
        await ReadJsonAsync(await client.GetAsync("/api/account/two-factor", Token));

    private static string[] Providers(JsonElement challenge) =>
        [.. challenge.GetProperty("providers").EnumerateArray().Select(provider => provider.GetString()!)];

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>(Token);

    /// <summary>가장 최근 로그인 확인 코드 메일의 6자리 코드. 아웃박스를 거쳐 오므로 <paramref name="count"/> 번째 메일이 올 때까지 기다린다.</summary>
    private async Task<string> ReadSignInCodeAsync(string email, int count = 1)
    {
        var query = Uri.EscapeDataString($"to:\"{email}\" subject:\"로그인 확인 코드\"");
        for (var attempt = 0; attempt < 50; attempt++)
        {
            var search = await _mailpit.GetFromJsonAsync<JsonElement>($"api/v1/search?query={query}", Token);
            if (search.GetProperty("messages").GetArrayLength() >= count)
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
