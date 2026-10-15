using AspNetCoreSkeletonApplication.Common.Dtos;
using AspNetCoreSkeletonApplication.Common.Persistence.Entities.Main;
using Riok.Mapperly.Abstractions;

namespace AspNetCoreSkeletonApplication.Common.Persistence.Mappers;

[Mapper]
internal static partial class ProductStatusHistoryEntityMapper
{
    public static partial ProductStatusHistoryDto ToDto(this ProductStatusHistoryEntity entity);

    public static partial IQueryable<ProductStatusHistoryDto> ProjectToDto(this IQueryable<ProductStatusHistoryEntity> query);

    /// <summary>식별자는 DB 가, 생성 필드는 인터셉터가 채운다.</summary>
    [MapperIgnoreTarget(nameof(ProductStatusHistoryEntity.Id))]
    [MapperIgnoreTarget(nameof(ProductStatusHistoryEntity.CreatedBy))]
    [MapperIgnoreTarget(nameof(ProductStatusHistoryEntity.CreatedAt))]
    public static partial ProductStatusHistoryEntity ToEntity(this ProductStatusHistoryCreateDto dto);
}
