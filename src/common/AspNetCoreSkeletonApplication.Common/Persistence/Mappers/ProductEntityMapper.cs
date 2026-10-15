using AspNetCoreSkeletonApplication.Common.Dtos;
using AspNetCoreSkeletonApplication.Common.Enums;
using AspNetCoreSkeletonApplication.Common.Persistence.Entities.Main;
using Riok.Mapperly.Abstractions;

namespace AspNetCoreSkeletonApplication.Common.Persistence.Mappers;

[Mapper]
internal static partial class ProductEntityMapper
{
    /// <summary>삭제 필드는 내보내지 않는다 — 지워진 행은 쿼리 필터가 이미 뺐다.</summary>
    [MapperIgnoreSource(nameof(ProductEntity.DeletedAt))]
    [MapperIgnoreSource(nameof(ProductEntity.DeletedBy))]
    public static partial ProductDto ToDto(this ProductEntity entity);

    /// <summary>프로젝션은 위 <see cref="ToDto"/> 의 설정을 그대로 따른다(RMG065).</summary>
    public static partial IQueryable<ProductDto> ProjectToDto(this IQueryable<ProductEntity> query);

    /// <summary>식별자는 DB 가, 감사 · 삭제 필드는 인터셉터가 채운다.</summary>
    [MapperIgnoreTarget(nameof(ProductEntity.Id))]
    [MapperIgnoreTarget(nameof(ProductEntity.CreatedBy))]
    [MapperIgnoreTarget(nameof(ProductEntity.CreatedAt))]
    [MapperIgnoreTarget(nameof(ProductEntity.UpdatedBy))]
    [MapperIgnoreTarget(nameof(ProductEntity.UpdatedAt))]
    [MapperIgnoreTarget(nameof(ProductEntity.DeletedAt))]
    [MapperIgnoreTarget(nameof(ProductEntity.DeletedBy))]
    public static partial ProductEntity ToEntity(this ProductCreateDto dto, ProductStatus status);
}
