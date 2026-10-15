using AspNetCoreSkeletonApplication.Common.Dtos;
using AspNetCoreSkeletonApplication.Common.Enums;
using AspNetCoreSkeletonApplication.Common.Persistence.Entities.Main;
using Riok.Mapperly.Abstractions;

namespace AspNetCoreSkeletonApplication.Common.Persistence.Mappers;

[Mapper]
internal static partial class ProductEntityMapper
{
    public static partial ProductDto ToDto(this ProductEntity entity);

    public static partial IQueryable<ProductDto> ProjectToDto(this IQueryable<ProductEntity> query);

    /// <summary>식별자는 DB 가, 감사 필드는 인터셉터가 채운다.</summary>
    [MapperIgnoreTarget(nameof(ProductEntity.Id))]
    [MapperIgnoreTarget(nameof(ProductEntity.CreatedBy))]
    [MapperIgnoreTarget(nameof(ProductEntity.CreatedAt))]
    [MapperIgnoreTarget(nameof(ProductEntity.UpdatedBy))]
    [MapperIgnoreTarget(nameof(ProductEntity.UpdatedAt))]
    public static partial ProductEntity ToEntity(this ProductCreateDto dto, ProductStatus status);
}
