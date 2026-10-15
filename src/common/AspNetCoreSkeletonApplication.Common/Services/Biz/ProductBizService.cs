using AspNetCoreSkeletonApplication.Common.Domain;
using AspNetCoreSkeletonApplication.Common.Dtos;
using AspNetCoreSkeletonApplication.Common.Enums;
using AspNetCoreSkeletonApplication.Common.Persistence;
using AspNetCoreSkeletonApplication.Common.Persistence.Services;

namespace AspNetCoreSkeletonApplication.Common.Services.Biz;

/// <summary>
/// 여러 앱이 함께 쓰는 상품 BizService. 조회·저장·호출 순서만 맡고, 상태를 바꿔도 되는지는
/// <see cref="ProductStatusTransition"/> 에 묻는다. 변경 메서드는 모두 <see cref="IMainTransaction"/> 안에서 돈다.
/// </summary>
public class ProductBizService(IProductEntityService productEntityService, IMainTransaction transaction)
{
    public Task<ProductDto> GetAsync(long id, CancellationToken cancellationToken) =>
        productEntityService.GetAsync(id, cancellationToken);

    public Task<IReadOnlyList<ProductDto>> ListAsync(CancellationToken cancellationToken) =>
        productEntityService.ListAsync(cancellationToken);

    public Task<ProductDto> CreateAsync(ProductCreateDto product, CancellationToken cancellationToken) =>
        transaction.ExecuteAsync(
            token => productEntityService.CreateAsync(product, ProductStatusTransition.Initial, token),
            cancellationToken);

    public Task<ProductDto> ChangeStatusAsync(long id, ProductStatus status, CancellationToken cancellationToken) =>
        transaction.ExecuteAsync(async token =>
        {
            var product = await productEntityService.GetAsync(id, token);
            ProductStatusTransition.EnsureCanChange(product.Status, status);
            return await productEntityService.UpdateStatusAsync(id, status, token);
        }, cancellationToken);
}
