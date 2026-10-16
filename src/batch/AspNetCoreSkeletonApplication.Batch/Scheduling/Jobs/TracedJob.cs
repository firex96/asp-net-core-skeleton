using System.Diagnostics;
using AspNetCoreSkeletonApplication.Common.Contexts;
using AspNetCoreSkeletonApplication.Common.Web;
using Quartz;

namespace AspNetCoreSkeletonApplication.Batch.Scheduling.Jobs;

/// <summary>
/// 모든 Job 의 기반. 실행마다 아래를 정한 뒤 <see cref="ExecuteJobAsync"/> 를 부른다 — HTTP 요청에서 미들웨어가 하는 일의 Job 판이다.
/// <list type="bullet">
/// <item>추적 ID — 수동 실행이 JobDataMap 에 넣은 <c>traceId</c>(검증), 없으면 새로</item>
/// <item>감사 주체 — 언제나 시스템(<c>0</c>). 배치가 쓴 행은 시스템이 쓴 것이다</item>
/// <item>로깅 scope — <c>X-Trace-Id</c> · <c>Job</c>, 그리고 시작 · 끝 · 실패 로그</item>
/// </list>
/// 실패는 다시 던진다 — Quartz 가 실행 이력에 실패로 남긴다.
/// </summary>
public abstract partial class TracedJob(ILogger logger) : IJob
{
    public const string TraceIdKey = "traceId";

    public async ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken)
    {
        context.MergedJobDataMap.TryGetString(TraceIdKey, out var requestedTraceId);
        var traceId = TraceIdSanitizer.Sanitize(requestedTraceId);
        var jobName = context.JobDetail.Key.Name;

        using (TraceIdContext.Begin(traceId))
        using (AuditorContext.Begin(null))
        using (logger.BeginScope("X-Trace-Id:{XTraceId} Job:{JobName}", traceId, jobName))
        {
            var started = Stopwatch.GetTimestamp();
            LogStarted(logger, jobName, context.FireInstanceId);
            try
            {
                await ExecuteJobAsync(context, cancellationToken);
                LogCompleted(logger, jobName, Stopwatch.GetElapsedTime(started).TotalMilliseconds);
            }
            catch (Exception exception)
            {
                LogFailed(logger, exception, jobName, Stopwatch.GetElapsedTime(started).TotalMilliseconds);
                throw;
            }
        }
    }

    protected abstract Task ExecuteJobAsync(IJobExecutionContext context, CancellationToken cancellationToken);

    [LoggerMessage(Level = LogLevel.Information, Message = "Job 시작 {JobName} ({FireInstanceId})")]
    private static partial void LogStarted(ILogger logger, string jobName, string fireInstanceId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Job 끝 {JobName} {ElapsedMilliseconds:F0}ms")]
    private static partial void LogCompleted(ILogger logger, string jobName, double elapsedMilliseconds);

    [LoggerMessage(Level = LogLevel.Error, Message = "Job 실패 {JobName} {ElapsedMilliseconds:F0}ms")]
    private static partial void LogFailed(ILogger logger, Exception exception, string jobName, double elapsedMilliseconds);
}
