using AspNetCoreSkeletonApplication.Batch.JobTrigger.Dtos;
using AspNetCoreSkeletonApplication.Batch.JobTrigger.Models;
using Riok.Mapperly.Abstractions;

namespace AspNetCoreSkeletonApplication.Batch.JobTrigger.Mappers;

[Mapper]
public static partial class JobTriggerModelMapper
{
    public static partial JobTriggerModel ToModel(this JobTriggerDto dto);
}
