using AspNetCoreSkeletonApplication.Batch.Configuration;
using AspNetCoreSkeletonApplication.Batch.Scheduling.Jobs;
using AspNetCoreSkeletonApplication.Common.OAuth.Services.Biz;
using Microsoft.Extensions.Options;
using Quartz;

namespace AspNetCoreSkeletonApplication.Batch.OAuth.Jobs;

/// <summary>
/// OAuth 서버(Server)가 쌓은 만료 · 폐기 토큰과 쓰이지 않는 동의를 지운다. 다시 실행해도 결과가 같다(지운 것은 대상에서 빠진다).
/// </summary>
[DisallowConcurrentExecution]
public sealed partial class PruneOAuthTokenJob(
    OAuthTokenBizService oauthTokenBizService,
    IOptions<PruneOAuthTokenJobOptions> options,
    TimeProvider timeProvider,
    ILogger<PruneOAuthTokenJob> logger) : TracedJob(logger)
{
    protected override async Task ExecuteJobAsync(IJobExecutionContext context, CancellationToken cancellationToken)
    {
        var pruned = await oauthTokenBizService.PruneAsync(timeProvider.GetUtcNow(), options.Value.RetentionDays, cancellationToken);

        context.Result = pruned;
        LogPruned(logger, pruned.Tokens, pruned.Authorizations);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "OAuth 저장소 정리 — 토큰 {Tokens}건, 동의 {Authorizations}건")]
    private static partial void LogPruned(ILogger logger, long tokens, long authorizations);
}
