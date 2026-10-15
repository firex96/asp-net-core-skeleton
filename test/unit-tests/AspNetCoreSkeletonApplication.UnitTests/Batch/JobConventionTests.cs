using AspNetCoreSkeletonApplication.Batch.Jobs;
using AspNetCoreSkeletonApplication.UnitTests.Conventions;
using Quartz;
using BatchProgram = AspNetCoreSkeletonApplication.Batch.Program;

namespace AspNetCoreSkeletonApplication.UnitTests.Batch;

/// <summary>
/// Job 규칙 (예외 없음) — <c>.Jobs</c> 의 <c>~Job</c> 은 <see cref="TracedJob"/> 을 상속해 추적 ID · 감사 주체 · 로그를 갖추고,
/// <see cref="DisallowConcurrentExecutionAttribute"/> 로 클러스터 전체에서 한 번에 하나만 돈다(스케줄 · 웹훅 · 대시보드 실행 모두).
/// </summary>
public class JobConventionTests
{
    private static readonly Type[] Jobs = typeof(BatchProgram).Assembly.GetTypes()
        .Where(type => type is { IsClass: true, IsAbstract: false } && typeof(IJob).IsAssignableFrom(type))
        .ToArray();

    [Fact]
    public void Jobs_DeriveFromTracedJob()
    {
        ConventionScan.AssertNoViolations(Jobs
            .Where(job => !job.IsSubclassOf(typeof(TracedJob)))
            .Select(job => $"{job.FullName} — TracedJob 을 상속한다"));
    }

    [Fact]
    public void Jobs_DisallowConcurrentExecution()
    {
        ConventionScan.AssertNoViolations(Jobs
            .Where(job => !job.IsDefined(typeof(DisallowConcurrentExecutionAttribute), inherit: true))
            .Select(job => $"{job.FullName} — [DisallowConcurrentExecution] 을 단다"));
    }

    [Fact]
    public void Jobs_LiveInJobsNamespace()
    {
        ConventionScan.AssertNoViolations(Jobs
            .Where(job => ConventionScan.LayerOf(job) != Layer.Job)
            .Select(job => $"{job.FullName} — .Jobs 네임스페이스에 둔다"));
    }
}
