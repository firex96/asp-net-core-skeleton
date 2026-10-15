using Quartz;

namespace AspNetCoreSkeletonApplication.Batch.Configuration;

/// <summary>Batch 앱이 스케줄할 Job 목록. <c>AddBatchScheduler</c> 의 인자로 채운다.</summary>
public sealed class BatchJobRegistry
{
    private readonly List<Type> _jobTypes = [];

    public IReadOnlyList<Type> JobTypes => _jobTypes;

    public BatchJobRegistry Add<TJob>() where TJob : IJob
    {
        _jobTypes.Add(typeof(TJob));
        return this;
    }
}
