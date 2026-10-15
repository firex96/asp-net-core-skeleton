using AspNetCoreSkeletonApplication.Batch.Configuration;
using AspNetCoreSkeletonApplication.Common.Services.Biz;
using Microsoft.Extensions.Options;
using Quartz;

namespace AspNetCoreSkeletonApplication.Batch.Jobs;

/// <summary>
/// 샘플 Job — 오래 손대지 않은 Draft 상품을 판매 종료한다. 판단 · 페이지 · 트랜잭션은 공유 <see cref="ProductBizService"/> 가 맡고,
/// Job 은 현재 시각과 구성을 넘기고 결과를 남길 뿐이다. 다시 실행해도 결과가 같다(처리한 상품은 대상에서 빠진다).
/// </summary>
[DisallowConcurrentExecution]
public sealed partial class DiscontinueStaleDraftJob(
    ProductBizService productBizService,
    IOptions<DiscontinueStaleDraftJobOptions> options,
    TimeProvider timeProvider,
    ILogger<DiscontinueStaleDraftJob> logger) : TracedJob(logger)
{
    protected override async Task ExecuteJobAsync(IJobExecutionContext context, CancellationToken cancellationToken)
    {
        var processed = await productBizService.DiscontinueStaleDraftsAsync(
            timeProvider.GetUtcNow(), options.Value.StaleAfterDays, options.Value.PageSize, cancellationToken);

        context.Result = processed;
        LogDiscontinued(logger, processed);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "판매 종료한 초안 {Count}건")]
    private static partial void LogDiscontinued(ILogger logger, int count);
}
