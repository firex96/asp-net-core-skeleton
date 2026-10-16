using AspNetCoreSkeletonApplication.Batch.Scheduling.Jobs;
using AspNetCoreSkeletonApplication.Common.Contexts;
using Microsoft.Extensions.Logging.Testing;
using NSubstitute;
using Quartz;

namespace AspNetCoreSkeletonApplication.UnitTests.Batch;

public class TracedJobTests
{
    [Fact]
    public async Task ValidRequestedTraceId_IsUsedAndAuditorIsSystem()
    {
        var job = new ProbeJob();

        await job.Execute(Context("webhook-trace-1"), TestContext.Current.CancellationToken);

        Assert.Equal("webhook-trace-1", job.SeenTraceId);
        Assert.Null(job.SeenAuditor);
        Assert.Equal(0, job.SeenAuditorOrSystem);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("forged\r\nline")]
    public async Task MissingOrInvalidTraceId_IsReplaced(string? requested)
    {
        var job = new ProbeJob();

        await job.Execute(Context(requested), TestContext.Current.CancellationToken);

        Assert.NotNull(job.SeenTraceId);
        Assert.NotEqual(requested, job.SeenTraceId);
        Assert.Matches("^[A-Za-z0-9._-]{1,64}$", job.SeenTraceId);
    }

    [Fact]
    public async Task Failure_IsRethrownAndContextsAreRestored()
    {
        var job = new ProbeJob { Fail = true };

        using (AuditorContext.Begin(77))
        {
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => job.Execute(Context("t-1"), TestContext.Current.CancellationToken).AsTask());

            Assert.Equal(77, AuditorContext.Current);
        }

        Assert.Null(TraceIdContext.Current);
    }

    private static IJobExecutionContext Context(string? traceId)
    {
        var data = new JobDataMap();
        if (traceId is not null)
        {
            data[TracedJob.TraceIdKey] = traceId;
        }

        var detail = Substitute.For<IJobDetail>();
        detail.Key.Returns(new JobKey(nameof(ProbeJob), "batch"));
        var context = Substitute.For<IJobExecutionContext>();
        context.MergedJobDataMap.Returns(data);
        context.JobDetail.Returns(detail);
        context.FireInstanceId.Returns("fire-1");
        return context;
    }

    private sealed class ProbeJob() : TracedJob(new FakeLogger<ProbeJob>())
    {
        public bool Fail { get; init; }

        public string? SeenTraceId { get; private set; }

        public long? SeenAuditor { get; private set; }

        public long SeenAuditorOrSystem { get; private set; }

        protected override Task ExecuteJobAsync(IJobExecutionContext context, CancellationToken cancellationToken)
        {
            SeenTraceId = TraceIdContext.Current;
            SeenAuditor = AuditorContext.Current;
            SeenAuditorOrSystem = AuditorContext.CurrentOrSystem;
            return Fail ? throw new InvalidOperationException("job failed") : Task.CompletedTask;
        }
    }
}
