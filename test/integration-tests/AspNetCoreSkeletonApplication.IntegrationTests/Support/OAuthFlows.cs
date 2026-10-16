using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Web;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.WebUtilities;

namespace AspNetCoreSkeletonApplication.IntegrationTests.Support;

/// <summary>OAuth 서버 테스트가 함께 쓰는 클라이언트 등록 · 인가 코드 + PKCE · 토큰 요청.</summary>
internal static class OAuthFlows
{
    public const string Callback = "https://client.test/callback";

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    /// <summary>구성 <c>Authentication:OAuthServer:Clients:{index}</c> 로 클라이언트를 등록한다(앱이 기동할 때 동기화한다).</summary>
    public static void RegisterClient(
        IWebHostBuilder builder, int index, string clientId, string consent, string[] grants, string[] scopes, string? secret = null)
    {
        var prefix = $"Authentication:OAuthServer:Clients:{index}:";
        builder.UseSetting(prefix + "ClientId", clientId);
        builder.UseSetting(prefix + "DisplayName", $"Test {clientId}");
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

    public static (string Verifier, string Challenge) Pkce()
    {
        var verifier = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        return (verifier, WebEncoders.Base64UrlEncode(SHA256.HashData(Encoding.ASCII.GetBytes(verifier))));
    }

    public static string AuthorizeUrl(string clientId, string scope, string challenge) =>
        $"/connect/authorize?client_id={clientId}&response_type=code&redirect_uri={Uri.EscapeDataString(Callback)}" +
        $"&scope={Uri.EscapeDataString(scope)}&state=st-1&code_challenge={challenge}&code_challenge_method=S256";

    /// <summary>로그인한 브라우저(쿠키)로 authorize 를 불러 콜백에 실린 코드를 꺼낸다.</summary>
    public static async Task<string> AuthorizeCodeAsync(HttpClient browser, string url)
    {
        var response = await browser.GetAsync(url, Token);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location!;
        Assert.StartsWith(Callback, location.AbsoluteUri, StringComparison.Ordinal);
        var query = HttpUtility.ParseQueryString(location.Query);
        Assert.Equal("st-1", query["state"]);
        return query["code"]!;
    }

    public static Task<HttpResponseMessage> PostTokenAsync(HttpClient client, Dictionary<string, string> form) =>
        client.PostAsync("/connect/token", new FormUrlEncodedContent(form), Token);

    public static async Task<JsonElement> TokenAsync(HttpClient client, Dictionary<string, string> form)
    {
        var response = await PostTokenAsync(client, form);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(Token);
        Assert.True(response.IsSuccessStatusCode, body.ToString());
        return body;
    }

    /// <summary>로그인한 브라우저로 인가 코드 + PKCE 를 끝까지 — 액세스 토큰을 돌려준다.</summary>
    public static async Task<string> UserAccessTokenAsync(HttpClient browser, HttpClient client, string clientId, string scope)
    {
        var pkce = Pkce();
        var code = await AuthorizeCodeAsync(browser, AuthorizeUrl(clientId, scope, pkce.Challenge));
        var tokens = await TokenAsync(client, new()
        {
            ["grant_type"] = "authorization_code", ["code"] = code, ["redirect_uri"] = Callback,
            ["client_id"] = clientId, ["code_verifier"] = pkce.Verifier,
        });
        return tokens.GetProperty("access_token").GetString()!;
    }
}
