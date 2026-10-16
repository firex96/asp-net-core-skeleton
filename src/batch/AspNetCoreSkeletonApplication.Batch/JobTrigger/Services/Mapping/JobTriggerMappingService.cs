using AspNetCoreSkeletonApplication.Batch.JobTrigger.Mappers;
using AspNetCoreSkeletonApplication.Batch.JobTrigger.Models;
using AspNetCoreSkeletonApplication.Batch.JobTrigger.Services.Biz;

namespace AspNetCoreSkeletonApplication.Batch.JobTrigger.Services.Mapping;

public class JobTriggerMappingService(JobTriggerBizService bizService)
{
    public async Task<JobTriggerModel> TriggerAsync(string jobName, JobTriggerRequestModel? request, CancellationToken cancellationToken) =>
        (await bizService.TriggerAsync(
            jobName, request?.Parameters ?? new Dictionary<string, string>(), cancellationToken)).ToModel();
}
