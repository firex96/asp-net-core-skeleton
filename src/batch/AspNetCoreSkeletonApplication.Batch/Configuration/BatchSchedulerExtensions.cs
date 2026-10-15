using AspNetCoreSkeletonApplication.Common.Configuration;
using Quartz;

namespace AspNetCoreSkeletonApplication.Batch.Configuration;

public static class BatchSchedulerExtensions
{
    /// <summary>Job · Trigger 의 그룹. 이름은 Job 클래스 이름, 트리거는 <c>{이름}.cron</c> 이다.</summary>
    public const string JobGroup = "batch";

    /// <summary>DB 의 SCHED_NAME. 템플릿이 프로젝트 이름으로 바꾼다.</summary>
    public const string SchedulerName = "asp-net-core-skeleton-application";

    /// <summary>
    /// Quartz 스케줄러를 등록한다 — SQL Server 영속 저장소(<c>quartz</c> 스키마) · 클러스터 · 실행 이력. 여러 인스턴스가 떠도
    /// 한 Job 은 클러스터 전체에서 한 번만 돈다. Job 마다 <c>Batch:Jobs:{Job 클래스 이름}</c> 의 <see cref="JobScheduleOptions"/> 를 읽어
    /// 트리거를 두고, 꺼진 Job 의 트리거는 기동할 때 지운다(<see cref="JobScheduleReconciler"/>).
    /// </summary>
    public static IServiceCollection AddBatchScheduler(
        this IServiceCollection services, IConfiguration configuration, Action<BatchJobRegistry> registerJobs)
    {
        var registry = new BatchJobRegistry();
        registerJobs(registry);
        services.AddSingleton(registry);

        var schedules = registry.JobTypes.ToDictionary(type => type.Name, type => ReadSchedule(configuration, type.Name));
        services.AddOptions<BatchOptions>().BindConfiguration("Batch");

        services.AddQuartz(quartz =>
        {
            // 스케줄러 이름은 같은 DB 를 쓰는 다른 서비스와 섞이지 않게 앱 이름으로, 노드 ID 는 클러스터 안에서 겹치지 않게 생성한다.
            quartz.ConfigureScheduler(scheduler =>
            {
                scheduler.InstanceName = SchedulerName;
                scheduler.GenerateInstanceId = true;
            });
            quartz.UsePersistentStore(store =>
            {
                store.UseSqlServer(configuration.GetRequiredConnectionString("Default"));
                store.ConfigureStore(options => options.TablePrefix = "quartz.QRTZ_");
                store.UseSystemTextJsonSerializer();
                store.UseClustering(_ => { });
                store.UseExecutionHistory();
            });

            foreach (var jobType in registry.JobTypes)
            {
                var jobName = jobType.Name;
                quartz.AddJob(jobType, job => job.WithIdentity(jobName, JobGroup).StoreDurably());

                var schedule = schedules[jobName];
                if (schedule.Enabled)
                {
                    quartz.AddTrigger(trigger => trigger
                        .WithIdentity($"{jobName}.cron", JobGroup)
                        .ForJob(jobName, JobGroup)
                        .WithCronSchedule(schedule.Cron!, cron => cron.WithMisfireInstruction(CronTriggerMisfireInstruction.FireAndProceed)));
                }
            }
        });

        services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);
        services.AddHostedService<JobScheduleReconciler>();

        return services;
    }

    public static string SectionOf(string jobName) => $"Batch:Jobs:{jobName}";

    /// <summary>스케줄 구성은 기동할 때 엄격하게 읽는다 — 섹션이 없거나, 켜졌는데 cron 이 없으면 멈춘다.</summary>
    private static JobScheduleOptions ReadSchedule(IConfiguration configuration, string jobName)
    {
        var section = configuration.GetSection(SectionOf(jobName));
        if (!section.Exists())
        {
            throw new InvalidOperationException($"구성 '{SectionOf(jobName)}' 가 없다. Enabled · Cron 을 적는다. README 「Batch」 참고.");
        }

        var schedule = section.Get<JobScheduleOptions>() ?? new JobScheduleOptions();
        if (schedule.Enabled && string.IsNullOrWhiteSpace(schedule.Cron))
        {
            throw new InvalidOperationException($"구성 '{SectionOf(jobName)}:Cron' 이 없다 — Enabled 인 Job 은 cron 이 필수다.");
        }

        return schedule;
    }
}
