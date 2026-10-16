using AspNetCoreSkeletonApplication.Batch.Product.Jobs;
using AspNetCoreSkeletonApplication.Batch.Scheduling;

namespace AspNetCoreSkeletonApplication.UnitTests.Batch;

/// <summary>대시보드는 이 앱의 도메인 Jobs 네임스페이스 타입만 다룬다 — 도메인 패키지로 옮겨도 샘플 Job 이 빠지지 않는다.</summary>
public class DashboardJobTypeFilterTests
{
    [Fact]
    public void AppJobs_InDomainJobsNamespaces_AreAllowed()
    {
        Assert.True(DashboardJobTypeFilter.IsAllowed(typeof(DiscontinueStaleDraftJob).FullName!));
        Assert.True(DashboardJobTypeFilter.IsAllowed(typeof(DiscontinueStaleDraftJob).AssemblyQualifiedName!));
    }

    [Theory]
    [InlineData("Quartz.Job.NativeJob, Quartz.Jobs")]
    [InlineData("AspNetCoreSkeletonApplication.Batch.Configuration.BatchJobRegistry")]
    [InlineData("AspNetCoreSkeletonApplication.BatchEvil.Product.Jobs.SomeJob")]
    [InlineData("Evil.AspNetCoreSkeletonApplication.Batch.Product.Jobs.SomeJob")]
    public void OtherTypes_AreRefused(string typeName)
    {
        Assert.False(DashboardJobTypeFilter.IsAllowed(typeName));
    }
}
