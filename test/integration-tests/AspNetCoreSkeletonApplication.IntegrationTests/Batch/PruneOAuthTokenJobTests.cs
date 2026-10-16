using AspNetCoreSkeletonApplication.Common.Persistence.Context;
using AspNetCoreSkeletonApplication.IntegrationTests.Support;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using Quartz;
using static OpenIddict.Abstractions.OpenIddictConstants;
using BatchProgram = AspNetCoreSkeletonApplication.Batch.Program;

namespace AspNetCoreSkeletonApplication.IntegrationTests.Batch;

/// <summary>
/// 토큰 정리 Job 을 Quartz 로 실제 실행한다 — 보관 기간이 지난, 더 쓸 수 없는 토큰(만료 · 폐기)만 지우고 쓸 수 있는 토큰과 최근 것은 남긴다.
/// 다시 실행해도 결과가 같다.
/// </summary>
[Collection(InfrastructureCollection.Name)]
public class PruneOAuthTokenJobTests(InfrastructureFixture infrastructure) : IAsyncLifetime
{
    private const string Database = "batch_prune_oauth";
    private static readonly JobKey Job = new("PruneOAuthTokenJob", "batch");

    private WebApplicationFactory<BatchProgram> _factory = null!;

    public async ValueTask InitializeAsync()
    {
        await infrastructure.RecreateDatabaseAsync(Database);
        _factory = new WebApplicationFactory<BatchProgram>().WithWebHostBuilder(builder =>
        {
            infrastructure.ConfigureApp(builder, Database);
            builder.UseSetting("Batch:Jobs:PruneOAuthTokenJob:Enabled", "false");
            builder.UseSetting("Batch:Jobs:DiscontinueStaleDraftJob:Enabled", "false");
        });
    }

    public async ValueTask DisposeAsync() => await _factory.DisposeAsync();

    [Fact]
    public async Task Prune_RemovesOnlyOldUnusableTokens_AndIsIdempotent()
    {
        var now = DateTimeOffset.UtcNow;
        _factory.CreateClient();
        var expiredOld = await SeedTokenAsync(Statuses.Valid, created: now.AddDays(-30), expires: now.AddDays(-29));
        var revokedOld = await SeedTokenAsync(Statuses.Revoked, created: now.AddDays(-30), expires: now.AddDays(1));
        var usable = await SeedTokenAsync(Statuses.Valid, created: now.AddDays(-30), expires: now.AddDays(1));
        var expiredRecent = await SeedTokenAsync(Statuses.Valid, created: now.AddDays(-2), expires: now.AddDays(-1));
        var scheduler = await StartedSchedulerAsync();

        await TriggerAndWaitAsync(scheduler, expectedExecutions: 1);
        var remaining = await RemainingTokenIdsAsync();
        Assert.DoesNotContain(expiredOld, remaining);
        Assert.DoesNotContain(revokedOld, remaining);
        Assert.Contains(usable, remaining);
        Assert.Contains(expiredRecent, remaining);

        await TriggerAndWaitAsync(scheduler, expectedExecutions: 2);
        Assert.Equal(remaining.Order(), (await RemainingTokenIdsAsync()).Order());
    }

    private async Task<string> SeedTokenAsync(string status, DateTimeOffset created, DateTimeOffset expires)
    {
        using var scope = _factory.Services.CreateScope();
        var applications = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
        var application = await applications.FindByClientIdAsync("prune-test", TestContext.Current.CancellationToken)
                          ?? await applications.CreateAsync(new OpenIddictApplicationDescriptor { ClientId = "prune-test", ClientType = ClientTypes.Public },
                              TestContext.Current.CancellationToken);
        var tokens = scope.ServiceProvider.GetRequiredService<IOpenIddictTokenManager>();
        var token = await tokens.CreateAsync(new OpenIddictTokenDescriptor
        {
            ApplicationId = await applications.GetIdAsync(application, TestContext.Current.CancellationToken),
            CreationDate = created,
            ExpirationDate = expires,
            Status = status,
            Subject = "1",
            Type = TokenTypeHints.AccessToken,
        }, TestContext.Current.CancellationToken);
        return (await tokens.GetIdAsync(token, TestContext.Current.CancellationToken))!;
    }

    private async Task<string[]> RemainingTokenIdsAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var tokens = scope.ServiceProvider.GetRequiredService<IOpenIddictTokenManager>();
        var ids = new List<string>();
        await foreach (var token in tokens.ListAsync(count: null, offset: null, TestContext.Current.CancellationToken))
        {
            ids.Add((await tokens.GetIdAsync(token, TestContext.Current.CancellationToken))!);
        }

        return [.. ids];
    }

    private async Task<IScheduler> StartedSchedulerAsync()
    {
        var scheduler = await _factory.Services.GetRequiredService<ISchedulerFactory>().GetScheduler(TestContext.Current.CancellationToken);
        await WaitUntilAsync(() => Task.FromResult(scheduler.Status == SchedulerStatus.Running));
        return scheduler;
    }

    private async Task TriggerAndWaitAsync(IScheduler scheduler, int expectedExecutions)
    {
        await scheduler.TriggerJob(Job, null, TestContext.Current.CancellationToken);
        await WaitUntilAsync(async () => await ExecutionsAsync(succeeded: true) + await ExecutionsAsync(succeeded: false) >= expectedExecutions);
        Assert.Equal(0, await ExecutionsAsync(succeeded: false));
    }

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
