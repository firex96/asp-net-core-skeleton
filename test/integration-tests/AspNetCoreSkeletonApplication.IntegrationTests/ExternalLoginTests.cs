using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AspNetCoreSkeletonApplication.Common.Persistence.Identity;
using AspNetCoreSkeletonApplication.IntegrationTests.Support;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace AspNetCoreSkeletonApplication.IntegrationTests;

/// <summary>
/// 소셜 로그인을 mock OAuth 서버(Google · Kakao · Naver 흉내)로 끝까지 — 앱의 POST 시작 → 공급자 authorize → 핸들러 콜백(state ·
/// correlation 검증, 코드 교환, userinfo) → 앱 콜백. 공급자별 사용자 정보 모양(Kakao 의 kakao_account, Naver 의 response)까지 실제 핸들러가 읽는다.
/// 연결된 계정만 로그인하고(자동 가입 없음), 연결은 로그인한 사용자만 한다.
/// </summary>
[Collection(InfrastructureCollection.Name)]
public class ExternalLoginTests(InfrastructureFixture infrastructure) : IAsyncLifetime
{
    private const string Database = "external_login";

    private WebApplicationFactory<Program> _factory = null!;
    private readonly HttpClient _browser = new(new HttpClientHandler { AllowAutoRedirect = false });

    public async ValueTask InitializeAsync()
    {
        await infrastructure.RecreateDatabaseAsync(Database);
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => infrastructure.ConfigureApp(builder, Database));
    }

    public async ValueTask DisposeAsync()
    {
        _browser.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Providers_ListsTheConfiguredOnes()
    {
        var providers = await _factory.CreateClient(TestUsers.Https).GetFromJsonAsync<JsonElement>("/api/auth/external/providers", Token);

        Assert.Equal(["Google", "Kakao", "Naver"], providers.EnumerateArray().Select(provider => provider.GetProperty("name").GetString()).Order());
    }

    [Fact]
    public async Task UnlinkedAccount_CannotLogIn()
    {
        var client = NewClient();

        var landing = await RoundTripAsync(client, "/api/auth/external/Google/login?returnUrl=%2Fproducts");

        Assert.Equal("/login?error=EXTERNAL_LOGIN_NOT_LINKED_ERROR&provider=Google", landing);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me", Token)).StatusCode);
    }

    [Theory]
    [InlineData("Google")]
    [InlineData("Kakao")]
    [InlineData("Naver")]
    public async Task LinkedAccount_LogsInAsTheSameUser(string provider)
    {
        var (owner, ownerId) = await SignInWithPasswordAsync($"{provider.ToLowerInvariant()}-owner@example.com");
        Assert.Equal($"/account?linked={provider}", await RoundTripAsync(owner, $"/api/auth/external/{provider}/link"));
        var logins = await owner.GetFromJsonAsync<JsonElement>("/api/auth/external/logins", Token);
        Assert.Contains(logins.EnumerateArray(), login => login.GetProperty("provider").GetString() == provider);

        var fresh = NewClient();
        var landing = await RoundTripAsync(fresh, $"/api/auth/external/{provider}/login?returnUrl=%2Fproducts%3Fpage%3D2");

        Assert.Equal("/products?page=2", landing);
        var me = await fresh.GetFromJsonAsync<JsonElement>("/api/auth/me", Token);
        Assert.Equal(ownerId, me.GetProperty("id").GetInt64());
    }

    [Fact]
    public async Task AccountLinkedToSomeoneElse_IsNotLinkedAgain()
    {
        var (first, _) = await SignInWithPasswordAsync("first@example.com");
        await RoundTripAsync(first, "/api/auth/external/Google/link");
        var (second, _) = await SignInWithPasswordAsync("second@example.com");

        var landing = await RoundTripAsync(second, "/api/auth/external/Google/link");

        Assert.Equal("/account?error=EXTERNAL_LOGIN_IN_USE_ERROR&provider=Google", landing);
        Assert.Empty((await second.GetFromJsonAsync<JsonElement>("/api/auth/external/logins", Token)).EnumerateArray());
    }

    [Fact]
    public async Task Unlink_StopsSocialLogin()
    {
        var (owner, _) = await SignInWithPasswordAsync("unlink@example.com");
        await RoundTripAsync(owner, "/api/auth/external/Naver/link");

        var unlink = await owner.DeleteAsync("/api/auth/external/Naver", Token);

        Assert.Equal(HttpStatusCode.NoContent, unlink.StatusCode);
        Assert.Equal("/login?error=EXTERNAL_LOGIN_NOT_LINKED_ERROR&provider=Naver",
            await RoundTripAsync(NewClient(), "/api/auth/external/Naver/login"));
    }

    [Fact]
    public async Task Unlink_TheLastLoginMethod_Is409()
    {
        const string email = "social-only@example.com";
        var (owner, _) = await SignInWithPasswordAsync(email);
        await RoundTripAsync(owner, "/api/auth/external/Kakao/link");
        using (var scope = _factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            Assert.True((await users.RemovePasswordAsync((await users.FindByEmailAsync(email))!)).Succeeded);
        }

        var unlink = await owner.DeleteAsync("/api/auth/external/Kakao", Token);

        Assert.Equal(HttpStatusCode.Conflict, unlink.StatusCode);
        Assert.Equal("LAST_LOGIN_METHOD_ERROR", (await unlink.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("code").GetString());
    }

    [Fact]
    public async Task LinkedButUnconfirmedAccount_IsAskedToConfirmTheEmail()
    {
        const string email = "unconfirmed-social@example.com";
        var (owner, _) = await SignInWithPasswordAsync(email);
        await RoundTripAsync(owner, "/api/auth/external/Kakao/link");
        using (var scope = _factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = (await users.FindByEmailAsync(email))!;
            user.EmailConfirmed = false;
            Assert.True((await users.UpdateAsync(user)).Succeeded);
        }

        var landing = await RoundTripAsync(NewClient(), "/api/auth/external/Kakao/login");

        Assert.Equal("/login?error=EMAIL_NOT_CONFIRMED_ERROR&provider=Kakao", landing);
    }

    [Fact]
    public async Task TwoFactorAccount_GoesToTheSecondStep_AfterSocialLogin()
    {
        var (owner, _) = await SignInWithPasswordAsync("social-two-factor@example.com");
        await RoundTripAsync(owner, "/api/auth/external/Naver/link");
        (await owner.PostAsync("/api/account/two-factor/email/enable", null, Token)).EnsureSuccessStatusCode();

        var browser = NewClient();
        var landing = await RoundTripAsync(browser, "/api/auth/external/Naver/login?returnUrl=%2Fproducts");

        Assert.Equal("/login?step=two-factor&returnUrl=%2Fproducts", landing);
        Assert.Equal(HttpStatusCode.Unauthorized, (await browser.GetAsync("/api/auth/me", Token)).StatusCode);
        var pending = await browser.GetFromJsonAsync<JsonElement>("/api/auth/login/2fa", Token);
        Assert.Equal("email", pending.GetProperty("providers")[0].GetString());
    }

    [Fact]
    public async Task UnknownProvider_Is404()
    {
        var response = await NewClient().PostAsync("/api/auth/external/Facebook/login", null, Token);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private HttpClient NewClient() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = TestUsers.Https.BaseAddress, AllowAutoRedirect = false });

    private async Task<(HttpClient Client, long UserId)> SignInWithPasswordAsync(string email)
    {
        var (signedIn, userId) = await TestUsers.SignInAsync(_factory, email, administrator: false);
        signedIn.Dispose();
        var client = NewClient();
        (await client.PostAsJsonAsync("/api/auth/login", new { email, password = TestUsers.Password }, Token)).EnsureSuccessStatusCode();
        return (client, userId);
    }

    /// <summary>
    /// 브라우저가 하는 왕복을 따라간다 — 앱 POST(302 → 공급자) → 공급자 authorize(302 → 앱의 signin-{공급자}) → 핸들러(302 → 앱 콜백)
    /// → 앱 콜백(302 → SPA 화면). 마지막 SPA 화면 주소를 돌려준다. 앱 쪽 요청은 같은 쿠키 상자(<paramref name="client"/>)로 보낸다.
    /// </summary>
    private async Task<string> RoundTripAsync(HttpClient client, string start)
    {
        var started = await client.PostAsync(start, null, Token);
        Assert.Equal(HttpStatusCode.Redirect, started.StatusCode);

        var authorized = await _browser.GetAsync(started.Headers.Location, Token);
        Assert.Equal(HttpStatusCode.Found, authorized.StatusCode);
        var signin = authorized.Headers.Location!;
        Assert.StartsWith("/api/auth/external/signin-", signin.AbsolutePath, StringComparison.Ordinal);

        var handled = await client.GetAsync(signin.PathAndQuery, Token);
        Assert.Equal(HttpStatusCode.Redirect, handled.StatusCode);
        Assert.Equal("/api/auth/external/callback", handled.Headers.Location!.OriginalString);

        var landed = await client.GetAsync(handled.Headers.Location, Token);
        Assert.Equal(HttpStatusCode.Redirect, landed.StatusCode);
        return landed.Headers.Location!.OriginalString;
    }
}
