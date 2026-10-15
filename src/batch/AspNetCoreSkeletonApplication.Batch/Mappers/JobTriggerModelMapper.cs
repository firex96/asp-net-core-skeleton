using AspNetCoreSkeletonApplication.Batch.Dtos;
using AspNetCoreSkeletonApplication.Batch.Models;
using Riok.Mapperly.Abstractions;

namespace AspNetCoreSkeletonApplication.Batch.Mappers;

[Mapper]
public static partial class JobTriggerModelMapper
{
    public static partial JobTriggerModel ToModel(this JobTriggerDto dto);
}
