using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using Quartz;
using BatchProgram = AspNetCoreSkeletonApplication.Batch.Program;

namespace AspNetCoreSkeletonApplication.FunctionalTests.Batch;

/// <summary>
/// Batch 앱을 DB 없이 띄운다 — 스케줄러는 대역(<see cref="Scheduler"/>)이라 아무 Job 도 실제로 돌지 않는다.
/// 웹훅 API 키: <c>ops</c>(DiscontinueStaleDraftJob · NoSuchJob 허용), <c>viewer</c>(허용 Job 없음).
/// </summary>
public class BatchWebApplicationFactory : WebApplicationFactory<BatchProgram>
{
    public const string OpsKey = "ops-key-0123456789abcdef0123456789abcdef";
    public const string ViewerKey = "viewer-key-0123456789abcdef0123456789abcd";

    public IScheduler Scheduler { get; } = Substitute.For<IScheduler>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Default", "Server=unused;Database=unused");
        builder.UseSetting("ConnectionStrings:Redis", "unused:6379");
        builder.UseSetting("Batch:Webhook:ApiKeys:0:Name", "ops");
        builder.UseSetting("Batch:Webhook:ApiKeys:0:Key", OpsKey);
        builder.UseSetting("Batch:Webhook:ApiKeys:0:AllowedJobs:0", "DiscontinueStaleDraftJob");
        builder.UseSetting("Batch:Webhook:ApiKeys:0:AllowedJobs:1", "NoSuchJob");
        builder.UseSetting("Batch:Webhook:ApiKeys:1:Name", "viewer");
        builder.UseSetting("Batch:Webhook:ApiKeys:1:Key", ViewerKey);
        builder.UseSetting("Batch:Jobs:DiscontinueStaleDraftJob:AllowedParameters:0", "dryRun");

        Scheduler.Exists(Arg.Is<JobKey>(key => key.Name == "DiscontinueStaleDraftJob"), Arg.Any<CancellationToken>()).Returns(true);
        builder.ConfigureTestServices(services =>
        {
            var factory = Substitute.For<ISchedulerFactory>();
            factory.GetScheduler(Arg.Any<CancellationToken>()).Returns(Scheduler);
            services.RemoveAll<ISchedulerFactory>();
            services.AddSingleton(factory);
        });
    }
}
