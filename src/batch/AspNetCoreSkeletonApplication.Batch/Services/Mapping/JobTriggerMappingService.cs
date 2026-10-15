using AspNetCoreSkeletonApplication.Batch.Mappers;
using AspNetCoreSkeletonApplication.Batch.Models;
using AspNetCoreSkeletonApplication.Batch.Services.Biz;

namespace AspNetCoreSkeletonApplication.Batch.Services.Mapping;

public class JobTriggerMappingService(JobTriggerBizService bizService)
{
    public async Task<JobTriggerModel> TriggerAsync(string jobName, JobTriggerRequestModel? request, CancellationToken cancellationToken) =>
        (await bizService.TriggerAsync(
            jobName, request?.Parameters ?? new Dictionary<string, string>(), cancellationToken)).ToModel();
}
