using System.Net;
using System.Net.Http.Json;
using AspNetCoreSkeletonApplication.Common.Contexts;
using AspNetCoreSkeletonApplication.Common.Errors;
using AspNetCoreSkeletonApplication.Common.Web;
using AspNetCoreSkeletonApplication.Server.Example.Clients;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;

namespace AspNetCoreSkeletonApplication.UnitTests.Clients;

/// <summary>
/// AddApiClient 로 등록한 실제 핸들러 체인(예외 변환 · resilience · 추적 ID)을 그대로 쓰고,
/// 맨 안쪽 네트워크만 가짜 <see cref="HttpMessageHandler"/> 로 바꾼다.
/// </summary>
public class ExampleApiClientTests
{
    [Fact]
    public async Task GetStatus_CallsRelativePathAndReadsBody()
    {
        var network = new FakeNetwork(_ => JsonResponse(HttpStatusCode.OK, new { status = "UP", checkedAt = "2026-10-15T00:00:00+00:00" }));

        var status = await CreateClient(network).GetStatusAsync(TestContext.Current.CancellationToken);

        Assert.Equal("UP", status.Status);
        Assert.Equal(new Uri("https://api.example.test/v1/status"), Assert.Single(network.Requests).RequestUri);
    }

    [Fact]
    public async Task GetStatus_PropagatesCurrentTraceId()
    {
        var network = new FakeNetwork(_ => JsonResponse(HttpStatusCode.OK, new { status = "UP", checkedAt = "2026-10-15T00:00:00+00:00" }));

        using (TraceIdContext.Begin("trace-from-request"))
        {
            await CreateClient(network).GetStatusAsync(TestContext.Current.CancellationToken);
        }

        Assert.Equal("trace-from-request", Assert.Single(Assert.Single(network.Requests).Headers.GetValues("X-Trace-Id")));
    }

    [Fact]
    public async Task GetStatus_ErrorStatus_ThrowsApiCallExceptionWithUpstreamStatus()
    {
        var network = new FakeNetwork(_ => new HttpResponseMessage(HttpStatusCode.NotFound));

        var exception = await Assert.ThrowsAsync<ApiCallException>(
            () => CreateClient(network).GetStatusAsync(TestContext.Current.CancellationToken));

        Assert.Equal(HttpStatusCode.NotFound, exception.UpstreamStatus);
        Assert.Equal(502, exception.ErrorCode.Status);
    }

    [Fact]
    public async Task GetStatus_NoResponseAfterRetries_ThrowsApiCallException()
    {
        var network = new FakeNetwork(_ => throw new HttpRequestException("connection refused"));

        var exception = await Assert.ThrowsAsync<ApiCallException>(
            () => CreateClient(network).GetStatusAsync(TestContext.Current.CancellationToken));

        Assert.Null(exception.UpstreamStatus);
        Assert.True(network.Requests.Count > 1, "표준 resilience 가 재시도해야 한다");
    }

    private static ExampleApiClient CreateClient(FakeNetwork network)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Clients:ExampleApi:BaseAddress"] = "https://api.example.test/v1/" })
            .Build());
        services.AddApiClient<ExampleApiClient, ExampleApiOptions>("Clients:ExampleApi")
            .ConfigurePrimaryHttpMessageHandler(() => network);
        // 재시도 대기를 없애 테스트를 빠르게 한다 — 재시도 횟수와 순서는 운영과 같다.
        services.ConfigureAll<HttpStandardResilienceOptions>(options => options.Retry.Delay = TimeSpan.Zero);

        return services.BuildServiceProvider().GetRequiredService<ExampleApiClient>();
    }

    private static HttpResponseMessage JsonResponse(HttpStatusCode status, object body) =>
        new(status) { Content = JsonContent.Create(body) };

    private sealed class FakeNetwork(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Task.FromResult(respond(request));
        }
    }
}
