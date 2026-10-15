using AspNetCoreSkeletonApplication.Common.Dtos;
using AspNetCoreSkeletonApplication.Common.Enums;

namespace AspNetCoreSkeletonApplication.Common.Persistence.Services;

/// <summary>상품 엔티티 하나의 CRUD. 밖으로는 Dto 만 주고받는다.</summary>
public interface IProductEntityService
{
    /// <summary>없으면 <c>ItemNotFoundException</c>(404).</summary>
    Task<ProductDto> GetAsync(long id, CancellationToken cancellationToken);

    Task<ProductDto?> FindAsync(long id, CancellationToken cancellationToken);

    Task<IReadOnlyList<ProductDto>> ListAsync(CancellationToken cancellationToken);

    Task<ProductDto> CreateAsync(ProductCreateDto product, ProductStatus status, CancellationToken cancellationToken);

    /// <summary>없으면 <c>ItemNotFoundException</c>(404). 전이 규칙은 보지 않는다 — 판단은 BizService 가 Domain 으로 한다.</summary>
    Task<ProductDto> UpdateStatusAsync(long id, ProductStatus status, CancellationToken cancellationToken);

    /// <summary>논리 삭제. 없으면(이미 지워졌어도) <c>ItemNotFoundException</c>(404).</summary>
    Task DeleteAsync(long id, CancellationToken cancellationToken);
}
