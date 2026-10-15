using Microsoft.Extensions.Options;
using Quartz;

namespace AspNetCoreSkeletonApplication.Batch.Configuration;

/// <summary>
/// 영속 저장소에는 예전에 둔 트리거가 남아 있다. 구성에서 꺼진(<see cref="JobScheduleOptions.Enabled"/> = false) Job 의 트리거를
/// 기동할 때 지워, 구성과 저장소가 어긋나지 않게 한다. 켜진 Job 의 트리거는 Quartz 가 구성대로 덮어쓴다.
/// </summary>
internal sealed class JobScheduleReconciler(
    ISchedulerFactory schedulerFactory,
    BatchJobRegistry registry,
    IOptionsMonitor<JobScheduleOptions> schedules) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var scheduler = await schedulerFactory.GetScheduler(cancellationToken);
        foreach (var jobName in registry.JobTypes.Select(type => type.Name).Where(name => !schedules.Get(name).Enabled))
        {
            await scheduler.UnscheduleJob(new TriggerKey($"{jobName}.cron", BatchSchedulerExtensions.JobGroup), cancellationToken);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
