using System.Net;
using AspNetCoreSkeletonApplication.FunctionalTests.Support;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AspNetCoreSkeletonApplication.FunctionalTests;

/// <summary>
/// 다른 출처의 브라우저 앱 — 구성 <c>AllowedOrigins</c> 의 출처에만, OAuth 엔드포인트(<c>/connect/*</c> · <c>/.well-known/*</c>)에만 CORS 를 연다.
/// 자격 증명(쿠키)은 허용하지 않는다.
/// </summary>
public class OAuthCorsTests(ProductWebApplicationFactory factory) : IClassFixture<ProductWebApplicationFactory>
{
    private const string AllowedOrigin = "https://app.client.test";

    private readonly WebApplicationFactory<Program> _factory = factory.WithWebHostBuilder(builder =>
        builder.UseSetting("Authentication:OAuthServer:AllowedOrigins:0", AllowedOrigin + "/"));

    [Fact]
    public async Task Preflight_ToTheTokenEndpoint_IsAllowedForAConfiguredOrigin_WithoutCredentials()
    {
        var response = await PreflightAsync("/connect/token", AllowedOrigin);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(AllowedOrigin, Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
        Assert.False(response.Headers.Contains("Access-Control-Allow-Credentials"));
    }

    [Fact]
    public async Task OtherOrigins_AndOtherPaths_GetNoCorsHeaders()
    {
        var otherOrigin = await PreflightAsync("/connect/token", "https://evil.example");
        var otherPath = await PreflightAsync("/api/products", AllowedOrigin);

        Assert.False(otherOrigin.Headers.Contains("Access-Control-Allow-Origin"));
        Assert.False(otherPath.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task Discovery_CarriesTheCorsHeader()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/.well-known/openid-configuration");
        request.Headers.Add("Origin", AllowedOrigin);

        var response = await _factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") }).SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(AllowedOrigin, Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
    }

    private Task<HttpResponseMessage> PreflightAsync(string path, string origin)
    {
        var request = new HttpRequestMessage(HttpMethod.Options, path);
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "content-type");
        return _factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") }).SendAsync(request, TestContext.Current.CancellationToken);
    }
}
