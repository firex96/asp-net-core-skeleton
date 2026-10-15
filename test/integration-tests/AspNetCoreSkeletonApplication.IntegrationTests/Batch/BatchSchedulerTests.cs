using System.Net;
using System.Net.Http.Headers;
using System.Text;
using AspNetCoreSkeletonApplication.IntegrationTests.Support;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using BatchProgram = AspNetCoreSkeletonApplication.Batch.Program;

namespace AspNetCoreSkeletonApplication.IntegrationTests.Batch;

/// <summary>Batch 앱의 스케줄러가 마이그레이션이 만든 quartz 스키마 위에서 클러스터 · 영속 저장소로 뜬다.</summary>
[Collection(InfrastructureCollection.Name)]
public class BatchSchedulerTests(InfrastructureFixture infrastructure) : IAsyncLifetime
{
    private const string Database = "batch_scheduler";
    private const string DashboardPassword = "dashboard-pass-0123456789";

    private WebApplicationFactory<BatchProgram> _factory = null!;

    public async ValueTask InitializeAsync()
    {
        await infrastructure.RecreateDatabaseAsync(Database);
        _factory = new WebApplicationFactory<BatchProgram>().WithWebHostBuilder(builder =>
        {
            infrastructure.ConfigureApp(builder, Database);
            builder.UseSetting("Batch:Dashboard:Username", "operator");
            builder.UseSetting("Batch:Dashboard:Password", DashboardPassword);
        });
    }

    public async ValueTask DisposeAsync() => await _factory.DisposeAsync();

    [Fact]
    public async Task Scheduler_StartsClusteredOnPersistentStore()
    {
        _factory.CreateClient(); // 호스트를 띄운다 — Quartz 는 앱 기동이 끝난 뒤 시작한다
        var scheduler = await _factory.Services.GetRequiredService<ISchedulerFactory>().GetScheduler(TestContext.Current.CancellationToken);
        await WaitUntilAsync(() => scheduler.Status == SchedulerStatus.Running);

        var metadata = await scheduler.GetMetadata(TestContext.Current.CancellationToken);
        Assert.True(metadata.JobStoreClustered);
        Assert.True(metadata.JobStorePersistent);
        Assert.Equal("asp-net-core-skeleton-application", metadata.SchedulerName);
        Assert.NotEqual("NON_CLUSTERED", metadata.SchedulerInstanceId);
    }

    [Theory]
    [InlineData("/quartz")]
    [InlineData("/quartz/jobs")]
    [InlineData("/_framework/blazor.web.js")]
    public async Task Dashboard_RendersForOperator(string path)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"operator:{DashboardPassword}")));

        var response = await _factory.CreateClient().SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        for (var attempt = 0; attempt < 100 && !condition(); attempt++)
        {
            await Task.Delay(100, TestContext.Current.CancellationToken);
        }

        Assert.True(condition());
    }
}
