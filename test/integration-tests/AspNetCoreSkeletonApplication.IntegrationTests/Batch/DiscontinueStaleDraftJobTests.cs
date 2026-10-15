using AspNetCoreSkeletonApplication.Common.Enums;
using AspNetCoreSkeletonApplication.Common.Persistence.Context;
using AspNetCoreSkeletonApplication.Common.Persistence.Entities.Main;
using AspNetCoreSkeletonApplication.IntegrationTests.Support;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using BatchProgram = AspNetCoreSkeletonApplication.Batch.Program;

namespace AspNetCoreSkeletonApplication.IntegrationTests.Batch;

/// <summary>
/// 샘플 Job 을 Quartz 로 실제 실행한다(cron 은 끄고 수동 실행). 재실행 멱등 — 형제 스켈레톤의 R-1~R-4 대응:
/// 1회차는 대상만 처리하고 이력을 남긴다, 2회차는 대상이 0건이고 데이터가 그대로다, 두 실행 모두 실행 이력에 성공으로 남는다.
/// </summary>
[Collection(InfrastructureCollection.Name)]
public class DiscontinueStaleDraftJobTests(InfrastructureFixture infrastructure) : IAsyncLifetime
{
    private const string Database = "batch_discontinue";
    private static readonly JobKey Job = new("DiscontinueStaleDraftJob", "batch");

    private WebApplicationFactory<BatchProgram> _factory = null!;

    public async ValueTask InitializeAsync()
    {
        await infrastructure.RecreateDatabaseAsync(Database);
        _factory = new WebApplicationFactory<BatchProgram>().WithWebHostBuilder(builder =>
        {
            infrastructure.ConfigureApp(builder, Database);
            builder.UseSetting("Batch:Jobs:DiscontinueStaleDraftJob:Enabled", "false");
        });
    }

    public async ValueTask DisposeAsync() => await _factory.DisposeAsync();

    [Fact]
    public async Task Rerun_IsIdempotent()
    {
        var now = DateTimeOffset.UtcNow;
        var staleA = await SeedAsync("stale-a", ProductStatus.Draft, now.AddDays(-40));
        var staleB = await SeedAsync("stale-b", ProductStatus.Draft, now.AddDays(-31));
        var fresh = await SeedAsync("fresh", ProductStatus.Draft, now.AddDays(-1));
        var onSale = await SeedAsync("on-sale", ProductStatus.OnSale, now.AddDays(-40));
        var scheduler = await StartedSchedulerAsync();

        // 1회차 — 오래된 Draft 둘만 판매 종료, 이력 둘
        await TriggerAndWaitAsync(scheduler, expectedExecutions: 1);
        var afterFirst = await SnapshotAsync();
        Assert.Equal(ProductStatus.Discontinued, afterFirst[staleA].Status);
        Assert.Equal(ProductStatus.Discontinued, afterFirst[staleB].Status);
        Assert.Equal(ProductStatus.Draft, afterFirst[fresh].Status);
        Assert.Equal(ProductStatus.OnSale, afterFirst[onSale].Status);
        Assert.Equal(2, await CountAsync(context => context.ProductStatusHistories));

        // 2회차 — 처리할 것이 없다, 데이터와 이력이 그대로
        await TriggerAndWaitAsync(scheduler, expectedExecutions: 2);
        Assert.Equal(afterFirst, await SnapshotAsync());
        Assert.Equal(2, await CountAsync(context => context.ProductStatusHistories));
        Assert.Equal(2, await SucceededExecutionsAsync());
    }

    [Fact]
    public async Task DisabledJob_HasNoTriggerButStaysRunnable()
    {
        var scheduler = await StartedSchedulerAsync();

        Assert.Null(await scheduler.GetTrigger(new TriggerKey("DiscontinueStaleDraftJob.cron", "batch"), TestContext.Current.CancellationToken));
        Assert.True(await scheduler.Exists(Job, TestContext.Current.CancellationToken));
    }

    private async Task<IScheduler> StartedSchedulerAsync()
    {
        _factory.CreateClient();
        var scheduler = await _factory.Services.GetRequiredService<ISchedulerFactory>().GetScheduler(TestContext.Current.CancellationToken);
        await WaitUntilAsync(() => Task.FromResult(scheduler.Status == SchedulerStatus.Running));
        return scheduler;
    }

    private async Task TriggerAndWaitAsync(IScheduler scheduler, int expectedExecutions)
    {
        await scheduler.TriggerJob(Job, null, TestContext.Current.CancellationToken);
        await WaitUntilAsync(async () => await SucceededExecutionsAsync() + await FailedExecutionsAsync() >= expectedExecutions);
        Assert.Equal(0, await FailedExecutionsAsync());
    }

    private async Task<long> SeedAsync(string name, ProductStatus status, DateTimeOffset updatedAt)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var entity = new ProductEntity { Name = name, Price = 1000, Status = status };
        dbContext.Products.Add(entity);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        // 인터셉터가 지금 시각을 넣었으므로, 오래된 행처럼 보이게 직접 고친다
        await dbContext.Products.Where(product => product.Id == entity.Id)
            .ExecuteUpdateAsync(setters => setters.SetProperty(product => product.UpdatedAt, updatedAt), TestContext.Current.CancellationToken);
        return entity.Id;
    }

    private async Task<Dictionary<long, (ProductStatus Status, DateTimeOffset UpdatedAt)>> SnapshotAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await dbContext.Products.ToDictionaryAsync(
            product => product.Id, product => (product.Status, product.UpdatedAt), TestContext.Current.CancellationToken);
    }

    private async Task<int> CountAsync(Func<AppDbContext, IQueryable<object>> query)
    {
        using var scope = _factory.Services.CreateScope();
        return await query(scope.ServiceProvider.GetRequiredService<AppDbContext>()).CountAsync(TestContext.Current.CancellationToken);
    }

    private Task<int> SucceededExecutionsAsync() => ExecutionsAsync(succeeded: true);

    private Task<int> FailedExecutionsAsync() => ExecutionsAsync(succeeded: false);

    private async Task<int> ExecutionsAsync(bool succeeded)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await dbContext.Database
            .SqlQuery<int>($"SELECT COUNT(*) AS [Value] FROM [quartz].[QRTZ_EXECUTION_HISTORY] WHERE [JOB_NAME] = {Job.Name} AND [SUCCEEDED] = {succeeded}")
            .SingleAsync(TestContext.Current.CancellationToken);
    }

    private static async Task WaitUntilAsync(Func<Task<bool>> condition)
    {
        for (var attempt = 0; attempt < 200; attempt++)
        {
            if (await condition())
            {
                return;
            }

            await Task.Delay(100, TestContext.Current.CancellationToken);
        }

        Assert.Fail("20초 안에 조건이 채워지지 않았다");
    }
}
