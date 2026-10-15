using System.Text.Json;
using AspNetCoreSkeletonApplication.FunctionalTests.Support;

namespace AspNetCoreSkeletonApplication.FunctionalTests;

/// <summary>X-Trace-Id 를 받아 검증해 쓰고, 응답 헤더와 에러 응답의 traceId 에 같은 값을 싣는다.</summary>
public class TraceIdTests(ErrorProbeWebApplicationFactory factory) : IClassFixture<ErrorProbeWebApplicationFactory>
{
    private const string Header = "X-Trace-Id";

    [Fact]
    public async Task ValidIncomingId_IsEchoed()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/weatherforecast");
        request.Headers.Add(Header, "gateway-trace-1");

        var response = await factory.CreateClient().SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal("gateway-trace-1", Assert.Single(response.Headers.GetValues(Header)));
    }

    [Fact]
    public async Task InvalidIncomingId_IsReplaced()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/weatherforecast");
        request.Headers.TryAddWithoutValidation(Header, "has space");

        var response = await factory.CreateClient().SendAsync(request, TestContext.Current.CancellationToken);

        var traceId = Assert.Single(response.Headers.GetValues(Header));
        Assert.NotEqual("has space", traceId);
        Assert.Matches("^[A-Za-z0-9._-]{1,64}$", traceId);
    }

    [Fact]
    public async Task ErrorResponse_TraceIdMatchesHeader()
    {
        var response = await factory.CreateClient().GetAsync("/error-probe/not-found", TestContext.Current.CancellationToken);

        var header = Assert.Single(response.Headers.GetValues(Header));
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.Equal(header, problem.RootElement.GetProperty("traceId").GetString());
    }
}
