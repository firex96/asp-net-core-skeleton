using AspNetCoreSkeletonApplication.Common.Product.Dtos;

namespace AspNetCoreSkeletonApplication.Common.Product.Persistence.Services;

/// <summary>상품 상태 이력. 추가 전용이라 Create 와 조회만 있다.</summary>
public interface IProductStatusHistoryEntityService
{
    Task<ProductStatusHistoryDto> CreateAsync(ProductStatusHistoryCreateDto history, CancellationToken cancellationToken);

    /// <summary>오래된 것부터.</summary>
    Task<IReadOnlyList<ProductStatusHistoryDto>> ListByProductAsync(long productId, CancellationToken cancellationToken);
}
