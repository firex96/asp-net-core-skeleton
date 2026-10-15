using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace AspNetCoreSkeletonApplication.FunctionalTests.Batch;

/// <summary>대시보드와 그 자산은 운영자(Basic 인증)만 — 계정이 없거나 틀리면 401 과 브라우저가 계정을 묻게 하는 헤더.</summary>
public class DashboardAuthenticationTests(BatchWebApplicationFactory factory) : IClassFixture<BatchWebApplicationFactory>
{
    [Theory]
    [InlineData("/quartz")]
    [InlineData("/_framework/blazor.web.js")]
    public async Task Anonymous_Is401WithBasicChallenge(string path)
    {
        var response = await factory.CreateClient().GetAsync(path, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("Basic", Assert.Single(response.Headers.WwwAuthenticate).Scheme);
    }

    [Fact]
    public async Task WrongPassword_Is401()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/quartz");
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{BatchWebApplicationFactory.DashboardUsername}:wrong")));

        var response = await factory.CreateClient().SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task WebhookKey_DoesNotOpenDashboard()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/quartz");
        request.Headers.Add("X-Api-Key", BatchWebApplicationFactory.OpsKey);

        var response = await factory.CreateClient().SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
