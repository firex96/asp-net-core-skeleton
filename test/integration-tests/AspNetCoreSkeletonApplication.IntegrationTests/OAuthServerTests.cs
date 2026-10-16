using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Web;
using AspNetCoreSkeletonApplication.IntegrationTests.Support;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.WebUtilities;

namespace AspNetCoreSkeletonApplication.IntegrationTests;

/// <summary>
/// OAuth 인가 서버 + OIDC 를 HTTP 로 끝까지 — 인가 코드 + PKCE → 토큰 → userinfo → 리프레시, 동의(Explicit) 저장, 클라이언트 자격 증명.
/// 클라이언트는 테스트 구성으로 등록한다(기동할 때 저장소와 맞춘다).
/// </summary>
[Collection(InfrastructureCollection.Name)]
public class OAuthServerTests(InfrastructureFixture infrastructure) : IAsyncLifetime
{
    private const string Database = "oauth_server";
    private const string Callback = OAuthFlows.Callback;
    private const string ServiceSecret = "test-service-secret";
    private const string BrowserAppOrigin = "https://app.client.test";

    private WebApplicationFactory<Program> _factory = null!;

    public async ValueTask InitializeAsync()
    {
        await infrastructure.RecreateDatabaseAsync(Database);
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            infrastructure.ConfigureApp(builder, Database);
            OAuthFlows.RegisterClient(builder, 0, "first-party", consent: "Implicit", grants: ["authorization_code", "refresh_token"],
                scopes: ["openid", "profile", "email", "offline_access", "products.manage"]);
            OAuthFlows.RegisterClient(builder, 1, "third-party", consent: "Explicit", grants: ["authorization_code"], scopes: ["openid", "email"]);
            OAuthFlows.RegisterClient(builder, 2, "service", consent: "Implicit", grants: ["client_credentials"], scopes: ["products.manage"], secret: ServiceSecret);
            builder.UseSetting("Authentication:OAuthServer:AllowedOrigins:0", BrowserAppOrigin);
        });
    }

    public async ValueTask DisposeAsync() => await _factory.DisposeAsync();

    [Fact]
    public async Task Discovery_AdvertisesOnlyWhatIsSupported()
    {
        var discovery = await NewClient().GetFromJsonAsync<JsonElement>("/.well-known/openid-configuration", Token);

        Assert.Equal($"{InfrastructureFixture.PublicOrigin}/", discovery.GetProperty("issuer").GetString());
        Assert.Equal(["code"], Strings(discovery, "response_types_supported"));
        Assert.Equal(["authorization_code", "client_credentials", "refresh_token"], Strings(discovery, "grant_types_supported").Order());
        Assert.Contains("products.manage", Strings(discovery, "scopes_supported"));
        Assert.Equal(["consent", "login", "none"], Strings(discovery, "prompt_values_supported").Order());
    }

    [Fact]
    public async Task PromptLogin_EndsTheSession_AndComesBackWithoutIt()
    {
        var (browser, _) = await SignInAsync("prompt-login@example.com");

        var response = await browser.GetAsync(OAuthFlows.AuthorizeUrl("first-party", "openid", OAuthFlows.Pkce().Challenge) + "&prompt=login", Token);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location!.OriginalString;
        Assert.StartsWith("/login?returnUrl=", location, StringComparison.Ordinal);
        var returnUrl = Uri.UnescapeDataString(location["/login?returnUrl=".Length..]);
        Assert.StartsWith("/connect/authorize?", returnUrl, StringComparison.Ordinal);
        Assert.DoesNotContain("prompt", returnUrl, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.Unauthorized, (await browser.GetAsync("/api/auth/me", Token)).StatusCode);
    }

    [Fact]
    public async Task PromptConsent_AsksEvenAnImplicitClient()
    {
        var (browser, _) = await SignInAsync("prompt-consent@example.com");
        var url = OAuthFlows.AuthorizeUrl("first-party", "openid", OAuthFlows.Pkce().Challenge) + "&prompt=consent";

        var asked = await browser.GetAsync(url, Token);

        var consentPage = asked.Headers.Location!.OriginalString;
        Assert.StartsWith("/consent?", consentPage, StringComparison.Ordinal);
        var parameters = QueryHelpers.ParseQuery(new Uri(new Uri("https://spa.test"), consentPage).Query)
            .ToDictionary(pair => pair.Key, pair => pair.Value.ToString());
        parameters["consent"] = "accept";
        var accepted = await browser.PostAsync("/connect/authorize", new FormUrlEncodedContent(parameters), Token);
        Assert.NotNull(HttpUtility.ParseQueryString(accepted.Headers.Location!.Query)["code"]);
    }

    [Fact]
    public async Task PromptSelectAccount_IsNotSupported()
    {
        var (browser, _) = await SignInAsync("prompt-select@example.com");

        var response = await browser.GetAsync(OAuthFlows.AuthorizeUrl("first-party", "openid", OAuthFlows.Pkce().Challenge) + "&prompt=select_account", Token);

        // prompt 검사는 redirect_uri 를 믿기 전에 하므로 클라이언트로 보내지 않고 400 으로 답한다.
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("invalid_request", await response.Content.ReadAsStringAsync(Token), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Authorize_WithoutSession_SendsToTheSpaLogin()
    {
        var response = await NewClient().GetAsync(OAuthFlows.AuthorizeUrl("first-party", "openid", OAuthFlows.Pkce().Challenge), Token);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location!.OriginalString;
        Assert.StartsWith("/login?returnUrl=", location, StringComparison.Ordinal);
        Assert.Contains("client_id=first-party", Uri.UnescapeDataString(location), StringComparison.Ordinal);
    }

    [Fact]
    public async Task CodeFlowWithPkce_IssuesTokens_UserInfo_AndRotatesRefreshTokens()
    {
        var (browser, userId) = await SignInAsync("code-flow@example.com");
        var pkce = OAuthFlows.Pkce();

        var code = await OAuthFlows.AuthorizeCodeAsync(browser, OAuthFlows.AuthorizeUrl("first-party", "openid profile email offline_access", pkce.Challenge));
        var tokens = await OAuthFlows.TokenAsync(NewClient(), new()
        {
            ["grant_type"] = "authorization_code", ["code"] = code, ["redirect_uri"] = Callback,
            ["client_id"] = "first-party", ["code_verifier"] = pkce.Verifier,
        });

        Assert.True(tokens.TryGetProperty("id_token", out _));
        var userInfo = await UserInfoAsync(tokens.GetProperty("access_token").GetString()!);
        Assert.Equal(userId.ToString(System.Globalization.CultureInfo.InvariantCulture), userInfo.GetProperty("sub").GetString());
        Assert.Equal("code-flow@example.com", userInfo.GetProperty("email").GetString());
        Assert.True(userInfo.GetProperty("email_verified").GetBoolean());
        var idToken = tokens.GetProperty("id_token").GetString()!;
        var idClaims = JsonDocument.Parse(Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlDecode(idToken.Split('.')[1])).RootElement;
        Assert.Equal("code-flow@example.com", idClaims.GetProperty("email").GetString());
        Assert.True(idClaims.GetProperty("email_verified").GetBoolean());

        var refreshToken = tokens.GetProperty("refresh_token").GetString();
        var refreshed = await OAuthFlows.TokenAsync(NewClient(), new() { ["grant_type"] = "refresh_token", ["refresh_token"] = refreshToken!, ["client_id"] = "first-party" });
        Assert.NotEqual(refreshToken, refreshed.GetProperty("refresh_token").GetString());
    }

    [Fact]
    public async Task BrowserAppOnAnotherOrigin_ExchangesTheCode_ThroughCors()
    {
        var (browser, _) = await SignInAsync("cors-app@example.com");
        var pkce = OAuthFlows.Pkce();
        var code = await OAuthFlows.AuthorizeCodeAsync(browser, OAuthFlows.AuthorizeUrl("first-party", "openid", pkce.Challenge));

        // 다른 출처의 SPA 가 fetch 로 보내는 요청 — cross-site 이고 쿠키가 없다(자격 증명을 싣지 않는다).
        var request = new HttpRequestMessage(HttpMethod.Post, "/connect/token")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code", ["code"] = code, ["redirect_uri"] = Callback,
                ["client_id"] = "first-party", ["code_verifier"] = pkce.Verifier,
            }),
        };
        request.Headers.Add("Origin", BrowserAppOrigin);
        request.Headers.Add("Sec-Fetch-Site", "cross-site");
        var response = await NewClient().SendAsync(request, Token);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(Token);
        Assert.True(response.IsSuccessStatusCode, body.ToString());
        Assert.True(body.TryGetProperty("access_token", out _));
        Assert.Equal(BrowserAppOrigin, Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
    }

    [Fact]
    public async Task CodeExchange_WithWrongVerifier_IsRejected()
    {
        var (browser, _) = await SignInAsync("pkce@example.com");
        var code = await OAuthFlows.AuthorizeCodeAsync(browser, OAuthFlows.AuthorizeUrl("first-party", "openid", OAuthFlows.Pkce().Challenge));

        var response = await OAuthFlows.PostTokenAsync(NewClient(), new()
        {
            ["grant_type"] = "authorization_code", ["code"] = code, ["redirect_uri"] = Callback,
            ["client_id"] = "first-party", ["code_verifier"] = OAuthFlows.Pkce().Verifier,
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("invalid_grant", (await response.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("error").GetString());
    }

    [Fact]
    public async Task ExplicitConsent_IsAskedOnce_ThenRemembered()
    {
        var (browser, _) = await SignInAsync("consent@example.com");
        var url = OAuthFlows.AuthorizeUrl("third-party", "openid email", OAuthFlows.Pkce().Challenge);

        var first = await browser.GetAsync(url, Token);
        Assert.Equal(HttpStatusCode.Redirect, first.StatusCode);
        var consentPage = first.Headers.Location!.OriginalString;
        Assert.StartsWith("/consent?", consentPage, StringComparison.Ordinal);

        var details = await browser.GetFromJsonAsync<JsonElement>("/api/oauth/consent?client_id=third-party&scope=openid%20email", Token);
        Assert.Equal("Test third-party", details.GetProperty("displayName").GetString());
        Assert.Equal(["openid", "email"], details.GetProperty("scopes").EnumerateArray().Select(scope => scope.GetProperty("name").GetString()));

        var parameters = QueryHelpers.ParseQuery(new Uri(new Uri("https://spa.test"), consentPage).Query)
            .ToDictionary(pair => pair.Key, pair => pair.Value.ToString());
        parameters["consent"] = "accept";
        var accepted = await browser.PostAsync("/connect/authorize", new FormUrlEncodedContent(parameters), Token);
        Assert.StartsWith(Callback, accepted.Headers.Location!.AbsoluteUri, StringComparison.Ordinal);
        Assert.NotNull(HttpUtility.ParseQueryString(accepted.Headers.Location.Query)["code"]);

        var again = await browser.GetAsync(url, Token);
        Assert.StartsWith(Callback, again.Headers.Location!.AbsoluteUri, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DeniedConsent_ReturnsAccessDeniedToTheClient()
    {
        var (browser, _) = await SignInAsync("deny@example.com");
        var consentPage = (await browser.GetAsync(OAuthFlows.AuthorizeUrl("third-party", "openid", OAuthFlows.Pkce().Challenge), Token)).Headers.Location!.OriginalString;
        var parameters = QueryHelpers.ParseQuery(new Uri(new Uri("https://spa.test"), consentPage).Query)
            .ToDictionary(pair => pair.Key, pair => pair.Value.ToString());
        parameters["consent"] = "deny";

        var denied = await browser.PostAsync("/connect/authorize", new FormUrlEncodedContent(parameters), Token);

        Assert.StartsWith(Callback, denied.Headers.Location!.AbsoluteUri, StringComparison.Ordinal);
        Assert.Equal("access_denied", HttpUtility.ParseQueryString(denied.Headers.Location.Query)["error"]);
    }

    [Fact]
    public async Task UnregisteredRedirectUri_IsNotRedirectedTo()
    {
        var (browser, _) = await SignInAsync("redirect@example.com");
        var url = OAuthFlows.AuthorizeUrl("first-party", "openid", OAuthFlows.Pkce().Challenge).Replace(Uri.EscapeDataString(Callback), Uri.EscapeDataString("https://evil.test/callback"), StringComparison.Ordinal);

        var response = await browser.GetAsync(url, Token);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Null(response.Headers.Location);
    }

    [Fact]
    public async Task ClientCredentials_IssueAToken_OnlyForPermittedScopes_AndRightSecret()
    {
        var granted = await OAuthFlows.PostTokenAsync(NewClient(), new()
        {
            ["grant_type"] = "client_credentials", ["client_id"] = "service", ["client_secret"] = ServiceSecret, ["scope"] = "products.manage",
        });
        var wrongSecret = await OAuthFlows.PostTokenAsync(NewClient(), new()
        {
            ["grant_type"] = "client_credentials", ["client_id"] = "service", ["client_secret"] = "wrong", ["scope"] = "products.manage",
        });
        var wrongScope = await OAuthFlows.PostTokenAsync(NewClient(), new()
        {
            ["grant_type"] = "client_credentials", ["client_id"] = "service", ["client_secret"] = ServiceSecret, ["scope"] = "authorization.manage",
        });

        Assert.Equal(HttpStatusCode.OK, granted.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, wrongSecret.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, wrongScope.StatusCode);
    }

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private HttpClient NewClient() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = TestUsers.Https.BaseAddress, AllowAutoRedirect = false });

    private async Task<(HttpClient Browser, long UserId)> SignInAsync(string email)
    {
        var (registered, userId) = await TestUsers.SignInAsync(_factory, email, administrator: false);
        registered.Dispose();
        var browser = NewClient();
        (await browser.PostAsJsonAsync("/api/auth/login", new { email, password = TestUsers.Password }, Token)).EnsureSuccessStatusCode();
        return (browser, userId);
    }

    private async Task<JsonElement> UserInfoAsync(string accessToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/connect/userinfo");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var response = await NewClient().SendAsync(request, Token);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(Token);
        Assert.True(response.IsSuccessStatusCode, body.ToString());
        return body;
    }

    private static string?[] Strings(JsonElement element, string property) =>
        element.GetProperty(property).EnumerateArray().Select(item => item.GetString()).ToArray();
}
