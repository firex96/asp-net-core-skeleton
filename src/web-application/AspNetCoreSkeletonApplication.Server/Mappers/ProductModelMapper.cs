using AspNetCoreSkeletonApplication.Common.Dtos;
using AspNetCoreSkeletonApplication.Server.Models;
using Riok.Mapperly.Abstractions;

namespace AspNetCoreSkeletonApplication.Server.Mappers;

[Mapper]
public static partial class ProductModelMapper
{
    /// <summary>감사 주체 ID 는 응답에 내보내지 않는다.</summary>
    [MapperIgnoreSource(nameof(ProductDto.CreatedBy))]
    [MapperIgnoreSource(nameof(ProductDto.UpdatedBy))]
    public static partial ProductModel ToModel(this ProductDto dto);

    public static partial IReadOnlyList<ProductModel> ToModels(this IReadOnlyList<ProductDto> dtos);

    public static partial ProductCreateDto ToDto(this ProductCreateModel model);
}
