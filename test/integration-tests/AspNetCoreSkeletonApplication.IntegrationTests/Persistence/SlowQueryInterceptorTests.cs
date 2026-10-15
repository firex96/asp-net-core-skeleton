using AspNetCoreSkeletonApplication.Common.Diagnostics;
using AspNetCoreSkeletonApplication.Common.Persistence.Interceptors;
using AspNetCoreSkeletonApplication.IntegrationTests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;

namespace AspNetCoreSkeletonApplication.IntegrationTests.Persistence;

/// <summary>임계값을 넘긴 DB 명령만 경고로 남는다 — 실제 SQL Server 명령으로 확인한다.</summary>
[Collection(InfrastructureCollection.Name)]
public class SlowQueryInterceptorTests(InfrastructureFixture database)
{
    [Fact]
    public async Task OverThreshold_LogsCommandText()
    {
        var logger = new FakeLogger<SlowQueryInterceptor>();

        await QueryAsync(TimeSpan.Zero, logger);

        var record = Assert.Single(logger.Collector.GetSnapshot(), record => record.Message.Contains("SELECT"));
        Assert.Equal(LogLevel.Warning, record.Level);
        Assert.StartsWith("느린 쿼리", record.Message);
    }

    [Fact]
    public async Task UnderThreshold_LogsNothing()
    {
        var logger = new FakeLogger<SlowQueryInterceptor>();

        await QueryAsync(TimeSpan.FromHours(1), logger);

        Assert.Empty(logger.Collector.GetSnapshot());
    }

    private async Task QueryAsync(TimeSpan threshold, FakeLogger<SlowQueryInterceptor> logger)
    {
        var interceptor = new SlowQueryInterceptor(
            new FixedOptionsMonitor(new DiagnosticsOptions { SlowQueryThreshold = threshold }), logger);
        await using var context = new DbContext(new DbContextOptionsBuilder()
            .UseSqlServer(database.ConnectionString)
            .AddInterceptors(interceptor)
            .Options);

        await context.Database.SqlQuery<int>($"SELECT 1 AS [Value]").ToListAsync(TestContext.Current.CancellationToken);
    }

    private sealed class FixedOptionsMonitor(DiagnosticsOptions value) : IOptionsMonitor<DiagnosticsOptions>
    {
        public DiagnosticsOptions CurrentValue => value;

        public DiagnosticsOptions Get(string? name) => value;

        public IDisposable? OnChange(Action<DiagnosticsOptions, string?> listener) => null;
    }
}
