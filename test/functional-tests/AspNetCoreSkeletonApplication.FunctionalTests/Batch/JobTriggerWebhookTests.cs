using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using NSubstitute;
using Quartz;

namespace AspNetCoreSkeletonApplication.FunctionalTests.Batch;

/// <summary>웹훅은 인가가 먼저다 — 키 없음 · 틀림 401, 허용되지 않은 Job 403, 받지 않는 파라미터 400, 그다음에야 실행(202).</summary>
public class JobTriggerWebhookTests(BatchWebApplicationFactory factory) : IClassFixture<BatchWebApplicationFactory>
{
    private const string TriggerPath = "/api/jobs/DiscontinueStaleDraftJob/trigger";

    [Theory]
    [InlineData(null)]
    [InlineData("wrong-key-0123456789abcdef0123456789abcdef")]
    public async Task MissingOrWrongKey_Is401(string? key)
    {
        var response = await SendAsync(TriggerPath, key, body: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("UNAUTHORIZED_ERROR", (await ReadJsonAsync(response)).GetProperty("code").GetString());
    }

    [Fact]
    public async Task KeyWithoutThatJob_Is403()
    {
        var response = await SendAsync(TriggerPath, BatchWebApplicationFactory.ViewerKey, body: null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("FORBIDDEN_ERROR", (await ReadJsonAsync(response)).GetProperty("code").GetString());
    }

    [Fact]
    public async Task UnlistedJobName_Is403NotLeakingExistence()
    {
        var response = await SendAsync("/api/jobs/SomeOtherJob/trigger", BatchWebApplicationFactory.OpsKey, body: null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ParameterNotAllowed_Is400WithoutEchoingValue()
    {
        var response = await SendAsync(TriggerPath, BatchWebApplicationFactory.OpsKey,
            new { parameters = new Dictionary<string, string> { ["traceId"] = "spoofed", ["dryRun"] = "true" } });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var detail = (await ReadJsonAsync(response)).GetProperty("detail").GetString();
        Assert.Contains("traceId", detail);
        Assert.DoesNotContain("spoofed", detail);
    }

    [Fact]
    public async Task AllowedButMissingJob_Is404()
    {
        var response = await SendAsync("/api/jobs/NoSuchJob/trigger", BatchWebApplicationFactory.OpsKey, body: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Allowed_Is202AndTriggersWithRequestTraceId()
    {
        factory.Scheduler.ClearReceivedCalls();

        var response = await SendAsync(TriggerPath, BatchWebApplicationFactory.OpsKey,
            new { parameters = new Dictionary<string, string> { ["dryRun"] = "true" } }, traceId: "ops-trace-1");

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Equal("ops-trace-1", (await ReadJsonAsync(response)).GetProperty("traceId").GetString());
        await factory.Scheduler.Received(1).TriggerJob(
            new JobKey("DiscontinueStaleDraftJob", "batch"),
            Arg.Is<JobDataMap>(data => Equals(data["traceId"], "ops-trace-1") && Equals(data["dryRun"], "true")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Allowed_WithoutBody_Is202()
    {
        var response = await SendAsync(TriggerPath, BatchWebApplicationFactory.OpsKey, body: null);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }

    private async Task<HttpResponseMessage> SendAsync(string path, string? apiKey, object? body, string? traceId = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, path);
        if (apiKey is not null)
        {
            request.Headers.Add("X-Api-Key", apiKey);
        }

        if (traceId is not null)
        {
            request.Headers.Add("X-Trace-Id", traceId);
        }

        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return await factory.CreateClient().SendAsync(request, TestContext.Current.CancellationToken);
    }

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).RootElement;
}
