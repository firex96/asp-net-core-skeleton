using AspNetCoreSkeletonApplication.Common.Diagnostics;
using AspNetCoreSkeletonApplication.Common.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;

namespace AspNetCoreSkeletonApplication.UnitTests.Web;

public class SlowRequestMiddlewareTests
{
    [Fact]
    public async Task OverThreshold_LogsWarningWithoutQueryString()
    {
        var logger = new FakeLogger<SlowRequestMiddleware>();

        await RunAsync(TimeSpan.Zero, logger);

        var record = Assert.Single(logger.Collector.GetSnapshot());
        Assert.Equal(LogLevel.Warning, record.Level);
        Assert.Contains("GET /api/products → 200", record.Message);
        Assert.DoesNotContain("secret", record.Message);
    }

    [Fact]
    public async Task UnderThreshold_LogsNothing()
    {
        var logger = new FakeLogger<SlowRequestMiddleware>();

        await RunAsync(TimeSpan.FromHours(1), logger);

        Assert.Empty(logger.Collector.GetSnapshot());
    }

    private static Task RunAsync(TimeSpan threshold, FakeLogger<SlowRequestMiddleware> logger)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/api/products";
        context.Request.QueryString = new QueryString("?token=secret");
        var middleware = new SlowRequestMiddleware(
            _ => Task.CompletedTask,
            new FixedOptionsMonitor(new DiagnosticsOptions { SlowRequestThreshold = threshold }),
            logger);
        return middleware.InvokeAsync(context);
    }

    private sealed class FixedOptionsMonitor(DiagnosticsOptions value) : IOptionsMonitor<DiagnosticsOptions>
    {
        public DiagnosticsOptions CurrentValue => value;

        public DiagnosticsOptions Get(string? name) => value;

        public IDisposable? OnChange(Action<DiagnosticsOptions, string?> listener) => null;
    }
}
