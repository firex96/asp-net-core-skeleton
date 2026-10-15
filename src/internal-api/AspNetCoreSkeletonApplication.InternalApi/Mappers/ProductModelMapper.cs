using AspNetCoreSkeletonApplication.Common.Dtos;
using AspNetCoreSkeletonApplication.InternalApi.Models;
using Riok.Mapperly.Abstractions;

namespace AspNetCoreSkeletonApplication.InternalApi.Mappers;

[Mapper]
public static partial class ProductModelMapper
{
    public static partial ProductModel ToModel(this ProductDto dto);
}
