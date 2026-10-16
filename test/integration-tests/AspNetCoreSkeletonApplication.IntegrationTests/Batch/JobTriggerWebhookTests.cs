using System.Net;
using AspNetCoreSkeletonApplication.Common.Constants;
using AspNetCoreSkeletonApplication.Common.Product.Enums;
using AspNetCoreSkeletonApplication.Common.Persistence.Context;
using AspNetCoreSkeletonApplication.Common.Product.Persistence.Entities;
using AspNetCoreSkeletonApplication.IntegrationTests.Support;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using BatchProgram = AspNetCoreSkeletonApplication.Batch.Program;

namespace AspNetCoreSkeletonApplication.IntegrationTests.Batch;

/// <summary>웹훅이 실제 스케줄러로 Job 을 돌린다 — 상품이 바뀌고, Quartz 실행 이력에 수동 실행 · 성공으로 남고, 감사 주체는 시스템이다.</summary>
[Collection(InfrastructureCollection.Name)]
public class JobTriggerWebhookTests(InfrastructureFixture infrastructure) : IAsyncLifetime
{
    private const string Database = "batch_webhook";
    private const string Key = "ops-key-0123456789abcdef0123456789abcdef";

    private WebApplicationFactory<BatchProgram> _factory = null!;

    public async ValueTask InitializeAsync()
    {
        await infrastructure.RecreateDatabaseAsync(Database);
        _factory = new WebApplicationFactory<BatchProgram>().WithWebHostBuilder(builder =>
        {
            infrastructure.ConfigureApp(builder, Database);
            builder.UseSetting("Batch:Jobs:DiscontinueStaleDraftJob:Enabled", "false");
            builder.UseSetting("Batch:Webhook:ApiKeys:0:Name", "ops");
            builder.UseSetting("Batch:Webhook:ApiKeys:0:Key", Key);
            builder.UseSetting("Batch:Webhook:ApiKeys:0:AllowedJobs:0", "DiscontinueStaleDraftJob");
        });
    }

    public async ValueTask DisposeAsync() => await _factory.DisposeAsync();

    [Fact]
    public async Task Webhook_RunsJobManually()
    {
        var productId = await SeedStaleDraftAsync();
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/jobs/DiscontinueStaleDraftJob/trigger");
        request.Headers.Add("X-Api-Key", Key);
        request.Headers.Add("X-Trace-Id", "webhook-it-1");

        var response = await _factory.CreateClient().SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        await WaitUntilAsync(async () => await ManualSucceededExecutionsAsync() == 1);
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var product = await dbContext.Products.SingleAsync(row => row.Id == productId, TestContext.Current.CancellationToken);
        var history = await dbContext.ProductStatusHistories.SingleAsync(row => row.ProductId == productId, TestContext.Current.CancellationToken);
        Assert.Equal(ProductStatus.Discontinued, product.Status);
        Assert.Equal(AuditorIds.System, history.CreatedBy);
    }

    private async Task<long> SeedStaleDraftAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var entity = new ProductEntity { Name = "stale", Price = 1000, Status = ProductStatus.Draft };
        dbContext.Products.Add(entity);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        await dbContext.Products.Where(product => product.Id == entity.Id)
            .ExecuteUpdateAsync(setters => setters.SetProperty(product => product.UpdatedAt, DateTimeOffset.UtcNow.AddDays(-90)), TestContext.Current.CancellationToken);
        return entity.Id;
    }

    private async Task<int> ManualSucceededExecutionsAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await dbContext.Database
            .SqlQuery<int>($"SELECT COUNT(*) AS [Value] FROM [quartz].[QRTZ_EXECUTION_HISTORY] WHERE [JOB_NAME] = {"DiscontinueStaleDraftJob"} AND [SUCCEEDED] = 1 AND [MANUAL] = 1")
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
