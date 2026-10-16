using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
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
    private const string Callback = "https://client.test/callback";
    private const string ServiceSecret = "test-service-secret";

    private WebApplicationFactory<Program> _factory = null!;

    public async ValueTask InitializeAsync()
    {
        await infrastructure.RecreateDatabaseAsync(Database);
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            infrastructure.ConfigureApp(builder, Database);
            Client(builder, 0, "first-party", consent: "Implicit", grants: ["authorization_code", "refresh_token"],
                scopes: ["openid", "profile", "email", "offline_access", "products.manage"]);
            Client(builder, 1, "third-party", consent: "Explicit", grants: ["authorization_code"], scopes: ["openid", "email"]);
            Client(builder, 2, "service", consent: "Implicit", grants: ["client_credentials"], scopes: ["products.manage"], secret: ServiceSecret);
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
    }

    [Fact]
    public async Task Authorize_WithoutSession_SendsToTheSpaLogin()
    {
        var response = await NewClient().GetAsync(AuthorizeUrl("first-party", "openid", Pkce().Challenge), Token);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location!.OriginalString;
        Assert.StartsWith("/login?returnUrl=", location, StringComparison.Ordinal);
        Assert.Contains("client_id=first-party", Uri.UnescapeDataString(location), StringComparison.Ordinal);
    }

    [Fact]
    public async Task CodeFlowWithPkce_IssuesTokens_UserInfo_AndRotatesRefreshTokens()
    {
        var (browser, userId) = await SignInAsync("code-flow@example.com");
        var pkce = Pkce();

        var code = await AuthorizeCodeAsync(browser, AuthorizeUrl("first-party", "openid profile email offline_access", pkce.Challenge));
        var tokens = await TokenAsync(new()
        {
            ["grant_type"] = "authorization_code", ["code"] = code, ["redirect_uri"] = Callback,
            ["client_id"] = "first-party", ["code_verifier"] = pkce.Verifier,
        });

        Assert.True(tokens.TryGetProperty("id_token", out _));
        var userInfo = await UserInfoAsync(tokens.GetProperty("access_token").GetString()!);
        Assert.Equal(userId.ToString(System.Globalization.CultureInfo.InvariantCulture), userInfo.GetProperty("sub").GetString());
        Assert.Equal("code-flow@example.com", userInfo.GetProperty("email").GetString());

        var refreshToken = tokens.GetProperty("refresh_token").GetString();
        var refreshed = await TokenAsync(new() { ["grant_type"] = "refresh_token", ["refresh_token"] = refreshToken!, ["client_id"] = "first-party" });
        Assert.NotEqual(refreshToken, refreshed.GetProperty("refresh_token").GetString());
    }

    [Fact]
    public async Task CodeExchange_WithWrongVerifier_IsRejected()
    {
        var (browser, _) = await SignInAsync("pkce@example.com");
        var code = await AuthorizeCodeAsync(browser, AuthorizeUrl("first-party", "openid", Pkce().Challenge));

        var response = await PostTokenAsync(new()
        {
            ["grant_type"] = "authorization_code", ["code"] = code, ["redirect_uri"] = Callback,
            ["client_id"] = "first-party", ["code_verifier"] = Pkce().Verifier,
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("invalid_grant", (await response.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("error").GetString());
    }

    [Fact]
    public async Task ExplicitConsent_IsAskedOnce_ThenRemembered()
    {
        var (browser, _) = await SignInAsync("consent@example.com");
        var url = AuthorizeUrl("third-party", "openid email", Pkce().Challenge);

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
        var consentPage = (await browser.GetAsync(AuthorizeUrl("third-party", "openid", Pkce().Challenge), Token)).Headers.Location!.OriginalString;
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
        var url = AuthorizeUrl("first-party", "openid", Pkce().Challenge).Replace(Uri.EscapeDataString(Callback), Uri.EscapeDataString("https://evil.test/callback"), StringComparison.Ordinal);

        var response = await browser.GetAsync(url, Token);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Null(response.Headers.Location);
    }

    [Fact]
    public async Task ClientCredentials_IssueAToken_OnlyForPermittedScopes_AndRightSecret()
    {
        var granted = await PostTokenAsync(new()
        {
            ["grant_type"] = "client_credentials", ["client_id"] = "service", ["client_secret"] = ServiceSecret, ["scope"] = "products.manage",
        });
        var wrongSecret = await PostTokenAsync(new()
        {
            ["grant_type"] = "client_credentials", ["client_id"] = "service", ["client_secret"] = "wrong", ["scope"] = "products.manage",
        });
        var wrongScope = await PostTokenAsync(new()
        {
            ["grant_type"] = "client_credentials", ["client_id"] = "service", ["client_secret"] = ServiceSecret, ["scope"] = "authorization.manage",
        });

        Assert.Equal(HttpStatusCode.OK, granted.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, wrongSecret.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, wrongScope.StatusCode);
    }

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static void Client(
        Microsoft.AspNetCore.Hosting.IWebHostBuilder builder, int index, string name, string consent, string[] grants, string[] scopes, string? secret = null)
    {
        var prefix = $"Authentication:OAuthServer:Clients:{index}:";
        builder.UseSetting(prefix + "ClientId", name);
        builder.UseSetting(prefix + "DisplayName", $"Test {name}");
        builder.UseSetting(prefix + "ConsentType", consent);
        builder.UseSetting(prefix + "RedirectUris:0", Callback);
        for (var i = 0; i < grants.Length; i++)
        {
            builder.UseSetting($"{prefix}GrantTypes:{i}", grants[i]);
        }

        for (var i = 0; i < scopes.Length; i++)
        {
            builder.UseSetting($"{prefix}Scopes:{i}", scopes[i]);
        }

        if (secret is not null)
        {
            builder.UseSetting(prefix + "ClientSecret", secret);
        }
    }

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

    private static string AuthorizeUrl(string clientId, string scope, string challenge) =>
        $"/connect/authorize?client_id={clientId}&response_type=code&redirect_uri={Uri.EscapeDataString(Callback)}" +
        $"&scope={Uri.EscapeDataString(scope)}&state=st-1&code_challenge={challenge}&code_challenge_method=S256";

    private static async Task<string> AuthorizeCodeAsync(HttpClient browser, string url)
    {
        var response = await browser.GetAsync(url, Token);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location!;
        Assert.StartsWith(Callback, location.AbsoluteUri, StringComparison.Ordinal);
        var query = HttpUtility.ParseQueryString(location.Query);
        Assert.Equal("st-1", query["state"]);
        return query["code"]!;
    }

    private Task<HttpResponseMessage> PostTokenAsync(Dictionary<string, string> form) =>
        NewClient().PostAsync("/connect/token", new FormUrlEncodedContent(form), Token);

    private async Task<JsonElement> TokenAsync(Dictionary<string, string> form)
    {
        var response = await PostTokenAsync(form);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(Token);
        Assert.True(response.IsSuccessStatusCode, body.ToString());
        return body;
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

    private static (string Verifier, string Challenge) Pkce()
    {
        var verifier = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        return (verifier, WebEncoders.Base64UrlEncode(SHA256.HashData(Encoding.ASCII.GetBytes(verifier))));
    }

    private static string?[] Strings(JsonElement element, string property) =>
        element.GetProperty(property).EnumerateArray().Select(item => item.GetString()).ToArray();
}
