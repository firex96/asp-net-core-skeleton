using AspNetCoreSkeletonApplication.Common.Domain;
using AspNetCoreSkeletonApplication.Common.Dtos;
using AspNetCoreSkeletonApplication.Common.Enums;
using AspNetCoreSkeletonApplication.Common.Persistence;
using AspNetCoreSkeletonApplication.Common.Persistence.Services;

namespace AspNetCoreSkeletonApplication.Common.Services.Biz;

/// <summary>
/// 여러 앱이 함께 쓰는 상품 BizService. 조회·저장·호출 순서만 맡고, 상태를 바꿔도 되는지는
/// <see cref="ProductStatusTransition"/> 에 묻는다. 변경 메서드는 모두 <see cref="IMainTransaction"/> 안에서 돈다.
/// 상태가 바뀌면 같은 트랜잭션에서 상태 이력을 남긴다 — 웹이든 배치든 이 메서드를 거친다.
/// </summary>
public class ProductBizService(
    IProductEntityService productEntityService,
    IProductStatusHistoryEntityService statusHistoryEntityService,
    IMainTransaction transaction)
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
            var changed = await productEntityService.UpdateStatusAsync(id, status, token);
            await statusHistoryEntityService.CreateAsync(new ProductStatusHistoryCreateDto(id, product.Status, status), token);
            return changed;
        }, cancellationToken);

    public Task DeleteAsync(long id, CancellationToken cancellationToken) =>
        transaction.ExecuteAsync(async token =>
        {
            await productEntityService.DeleteAsync(id, token);
            return true;
        }, cancellationToken);

    /// <summary>상품이 없으면 404 — 빈 목록으로 "없는 상품" 과 "이력 없는 상품" 을 구분할 수 없게 하지 않는다.</summary>
    public async Task<IReadOnlyList<ProductStatusHistoryDto>> ListStatusHistoryAsync(long id, CancellationToken cancellationToken)
    {
        await productEntityService.GetAsync(id, cancellationToken);
        return await statusHistoryEntityService.ListByProductAsync(id, cancellationToken);
    }
}
