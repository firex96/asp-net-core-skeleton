using AspNetCoreSkeletonApplication.Common.Product.Dtos;
using AspNetCoreSkeletonApplication.Server.Product.Models;
using Riok.Mapperly.Abstractions;

namespace AspNetCoreSkeletonApplication.Server.Product.Mappers;

[Mapper]
public static partial class ProductModelMapper
{
    /// <summary>작성자 ID 는 내보낸다(본인 상품을 지울 수 있는지 화면이 안다). 마지막 수정자는 내보내지 않는다.</summary>
    [MapperIgnoreSource(nameof(ProductDto.UpdatedBy))]
    public static partial ProductModel ToModel(this ProductDto dto);

    public static partial IReadOnlyList<ProductModel> ToModels(this IReadOnlyList<ProductDto> dtos);

    public static partial ProductCreateDto ToDto(this ProductCreateModel model);

    /// <summary>상품 ID 는 경로에 있고, 감사 주체 ID 는 내보내지 않는다.</summary>
    [MapperIgnoreSource(nameof(ProductStatusHistoryDto.ProductId))]
    [MapperIgnoreSource(nameof(ProductStatusHistoryDto.CreatedBy))]
    public static partial ProductStatusHistoryModel ToModel(this ProductStatusHistoryDto dto);

    public static partial IReadOnlyList<ProductStatusHistoryModel> ToModels(this IReadOnlyList<ProductStatusHistoryDto> dtos);
}
