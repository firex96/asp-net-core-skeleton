using AspNetCoreSkeletonApplication.Batch.Configuration;
using AspNetCoreSkeletonApplication.Batch.JobTrigger.Dtos;
using AspNetCoreSkeletonApplication.Batch.Scheduling.Jobs;
using AspNetCoreSkeletonApplication.Common.Contexts;
using AspNetCoreSkeletonApplication.Common.Errors;
using Microsoft.Extensions.Options;
using Quartz;

namespace AspNetCoreSkeletonApplication.Batch.JobTrigger.Services.Biz;

/// <summary>
/// Job 을 지금 한 번 실행한다. Quartz 의 <c>TriggerJob</c> 을 거치므로 스케줄 실행과 같은 규칙을 탄다 —
/// <c>[DisallowConcurrentExecution]</c>(클러스터 전체에서 한 번에 하나), 실행 이력(수동 실행으로 표시). 요청의 추적 ID 를 Job 에 넘긴다.
/// </summary>
public class JobTriggerBizService(ISchedulerFactory schedulerFactory, IOptions<BatchOptions> options)
{
    public async Task<JobTriggerDto> TriggerAsync(
        string jobName, IReadOnlyDictionary<string, string> parameters, CancellationToken cancellationToken)
    {
        var allowed = options.Value.Jobs.TryGetValue(jobName, out var schedule) ? schedule.AllowedParameters : [];
        var rejected = parameters.Keys.Where(key => !allowed.Contains(key, StringComparer.Ordinal)).ToArray();
        if (rejected.Length > 0)
        {
            // 값은 담지 않는다 — 거부된 요청의 값이 로그에 남지 않게.
            throw new ClientBadRequestException($"{jobName} 이 받지 않는 파라미터: {string.Join(", ", rejected)}");
        }

        var scheduler = await schedulerFactory.GetScheduler(cancellationToken);
        var jobKey = new JobKey(jobName, BatchSchedulerExtensions.JobGroup);
        if (!await scheduler.Exists(jobKey, cancellationToken))
        {
            throw new ItemNotFoundException($"Job {jobName} 이 없다");
        }

        var traceId = TraceIdContext.Current ?? throw new InvalidOperationException("추적 ID 가 없다 — TraceIdMiddleware 뒤에서만 부른다");
        var data = new JobDataMap();
        foreach (var (key, value) in parameters)
        {
            data[key] = value;
        }

        data[TracedJob.TraceIdKey] = traceId;
        await scheduler.TriggerJob(jobKey, data, cancellationToken);
        return new JobTriggerDto(jobName, traceId);
    }
}
