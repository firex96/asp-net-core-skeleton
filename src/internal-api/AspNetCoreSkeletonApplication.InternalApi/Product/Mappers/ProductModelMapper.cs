using AspNetCoreSkeletonApplication.Common.Product.Dtos;
using AspNetCoreSkeletonApplication.InternalApi.Product.Models;
using Riok.Mapperly.Abstractions;

namespace AspNetCoreSkeletonApplication.InternalApi.Product.Mappers;

[Mapper]
public static partial class ProductModelMapper
{
    public static partial ProductModel ToModel(this ProductDto dto);
}
