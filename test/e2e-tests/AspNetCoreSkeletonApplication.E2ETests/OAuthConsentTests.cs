using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Web;
using AspNetCoreSkeletonApplication.E2ETests.Support;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace AspNetCoreSkeletonApplication.E2ETests;

/// <summary>
/// 외부 앱의 인가 코드 흐름을 브라우저로 — authorize → SPA 로그인 → SPA 동의 화면 → 콜백의 코드 → 토큰. 콜백 주소(외부 앱)로 가는 요청에서 코드를 읽는다.
/// </summary>
public class OAuthConsentTests(E2EFixture fixture) : BrowserTest(fixture)
{
    [Theory]
    [MemberData(nameof(SpaClient.Names), MemberType = typeof(SpaClient))]
    public async Task ThirdPartyApp_GetsATokenAfterSignInAndConsent(string spa)
    {
        var server = await Fixture.ServerAsync(spa);
        var email = NewEmail(spa, "oauth");
        await RegisterAsync(server, email);
        var page = await OpenAsync(server);

        var verifier = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var challenge = WebEncoders.Base64UrlEncode(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
        await page.GotoAsync(
            $"/connect/authorize?client_id={SpaServer.OAuthClientId}&response_type=code&redirect_uri={Uri.EscapeDataString(SpaServer.OAuthCallback.AbsoluteUri)}" +
            $"&scope={Uri.EscapeDataString("openid email")}&state=st-1&code_challenge={challenge}&code_challenge_method=S256");

        await page.WaitForURLAsync("**/login?**");
        await page.GetByLabel("이메일").FillAsync(email);
        await page.GetByLabel("비밀번호", new() { Exact = true }).FillAsync(Password);
        await page.GetByRole(AriaRole.Button, new() { Name = "로그인", Exact = true }).ClickAsync();

        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Sample App에 접근을 허용할까요?" })).ToBeVisibleAsync();
        // 콜백(외부 앱)으로 가는 요청만 본다 — 리다이렉트로 이어진 요청이라 route 로는 가로채지 못하고, 그 주소는 실제로 열리지 않는다.
        var redirected = await page.RunAndWaitForRequestAsync(
            () => page.GetByRole(AriaRole.Button, new() { Name = "허용" }).ClickAsync(),
            request => request.Url.StartsWith(SpaServer.OAuthCallback.AbsoluteUri, StringComparison.Ordinal));
        var query = HttpUtility.ParseQueryString(new Uri(redirected.Url).Query);
        Assert.Equal("st-1", query["state"]);

        using var api = server.CreateApiClient();
        var token = await api.PostAsync("/connect/token", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code", ["code"] = query["code"]!, ["redirect_uri"] = SpaServer.OAuthCallback.AbsoluteUri,
            ["client_id"] = SpaServer.OAuthClientId, ["code_verifier"] = verifier,
        }), TestContext.Current.CancellationToken);
        var body = await token.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.True(token.IsSuccessStatusCode, body.ToString());
        Assert.False(string.IsNullOrEmpty(body.GetProperty("access_token").GetString()));
    }
}
