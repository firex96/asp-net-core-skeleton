using AspNetCoreSkeletonApplication.Common.OAuth.Services.Biz;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AspNetCoreSkeletonApplication.Common.Web.OAuth;

/// <summary>
/// Batch 없이 배포한 앱의 OAuth 저장소 정리 — <see cref="OAuthTokenPruningOptions.RunInServer"/> 일 때만 돈다. 정리는 Batch Job 과 같은
/// <see cref="OAuthTokenBizService.PruneAsync"/> 이고, 다시 실행해도 결과가 같아 여러 인스턴스가 함께 돌아도 된다.
/// </summary>
internal sealed partial class OAuthTokenPruneWorker(
    IServiceScopeFactory scopeFactory, IOptions<OAuthTokenPruningOptions> options, TimeProvider timeProvider, ILogger<OAuthTokenPruneWorker> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.RunInServer)
        {
            return;
        }

        using var timer = new PeriodicTimer(options.Value.Interval, timeProvider);
        try
        {
            do
            {
                await PruneAsync(stoppingToken);
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // 종료
        }
    }

    private async Task PruneAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var pruned = await scope.ServiceProvider.GetRequiredService<OAuthTokenBizService>()
                .PruneAsync(timeProvider.GetUtcNow(), options.Value.RetentionDays, cancellationToken);
            LogPruned(pruned.Tokens, pruned.Authorizations);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // DB 장애 등 — 루프를 멈추지 않는다. 다음 주기에 다시 한다.
            LogFailed(exception);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "OAuth 저장소 정리(Server) — 토큰 {Tokens}건, 동의 {Authorizations}건")]
    private partial void LogPruned(long tokens, long authorizations);

    [LoggerMessage(Level = LogLevel.Error, Message = "OAuth 저장소를 정리하지 못했다")]
    private partial void LogFailed(Exception exception);
}
