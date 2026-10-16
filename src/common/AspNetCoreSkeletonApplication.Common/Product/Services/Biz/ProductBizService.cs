using AspNetCoreSkeletonApplication.Common.Errors;
using AspNetCoreSkeletonApplication.Common.Product.Domain;
using AspNetCoreSkeletonApplication.Common.Product.Dtos;
using AspNetCoreSkeletonApplication.Common.Product.Enums;
using AspNetCoreSkeletonApplication.Common.Persistence;
using AspNetCoreSkeletonApplication.Common.Product.Persistence.Services;

namespace AspNetCoreSkeletonApplication.Common.Product.Services.Biz;

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
    /// <summary>보는 사람과 무관하게 읽는다 — 게이트웨이 뒤 내부 API(InternalApi) · 배치가 쓴다.</summary>
    public Task<ProductDto> GetAsync(long id, CancellationToken cancellationToken) =>
        productEntityService.GetAsync(id, cancellationToken);

    /// <summary>
    /// <paramref name="viewer"/> 가 볼 수 있는 상품 하나(<see cref="ProductAccessRule.CanView"/>). 볼 수 없으면 없는 상품과 같은 404 다 —
    /// 남의 판매 전 상품이 있다는 것도 드러내지 않는다.
    /// </summary>
    public async Task<ProductDto> GetVisibleAsync(long id, ProductViewerDto viewer, CancellationToken cancellationToken)
    {
        var product = await productEntityService.GetAsync(id, cancellationToken);
        return ProductAccessRule.CanView(product.Status, product.CreatedBy, viewer.ActorId, viewer.CanManageProducts)
            ? product
            : throw new ItemNotFoundException($"상품 {id} 가 없다");
    }

    /// <summary>
    /// <paramref name="viewer"/> 가 볼 수 있는 상품 목록. 목록은 페이지 없이 통째로 캐시돼 있어, 캐시된 목록에 Domain 규칙을 그대로 적용한다 —
    /// 규칙을 SQL 조건으로 한 번 더 쓰지 않는다. 목록에 페이지가 생기면 이 거르기를 쿼리로 옮긴다.
    /// </summary>
    public async Task<IReadOnlyList<ProductDto>> ListAsync(ProductViewerDto viewer, CancellationToken cancellationToken) =>
        (await productEntityService.ListAsync(cancellationToken))
        .Where(product => ProductAccessRule.CanView(product.Status, product.CreatedBy, viewer.ActorId, viewer.CanManageProducts))
        .ToList();

    public Task<ProductDto> CreateAsync(ProductCreateDto product, CancellationToken cancellationToken) =>
        transaction.ExecuteAsync(
            token => productEntityService.CreateAsync(product, ProductStatusTransition.Initial, token),
            cancellationToken);

    public Task<ProductDto> ChangeStatusAsync(long id, ProductStatus status, CancellationToken cancellationToken) =>
        transaction.ExecuteAsync(
            async token => await ChangeStatusCoreAsync(await productEntityService.GetAsync(id, token), status, token),
            cancellationToken);

    /// <summary>
    /// 오래 손대지 않은 Draft 상품을 판매 종료한다(Batch 의 <c>DiscontinueStaleDraftJob</c>). keyset 페이지로 읽고 페이지마다 커밋한다 —
    /// 처리한 행은 조회 조건(Draft)에서 빠지므로 다시 실행해도 같은 행을 두 번 처리하지 않는다. 처리한 건수를 돌려준다.
    /// </summary>
    public async Task<int> DiscontinueStaleDraftsAsync(
        DateTimeOffset now, int staleAfterDays, int pageSize, CancellationToken cancellationToken)
    {
        var cutoff = StaleDraftPolicy.CutoffOf(now, staleAfterDays);
        var processed = 0;
        var afterId = 0L;
        while (true)
        {
            var page = await productEntityService.ListStaleDraftsAsync(cutoff, afterId, pageSize, cancellationToken);
            if (page.Count == 0)
            {
                return processed;
            }

            await transaction.ExecuteAsync(async token =>
            {
                foreach (var product in page)
                {
                    await ChangeStatusCoreAsync(product, ProductStatus.Discontinued, token);
                }

                return page.Count;
            }, cancellationToken);
            processed += page.Count;
            afterId = page[^1].Id;
        }
    }

    public Task DeleteAsync(long id, CancellationToken cancellationToken) =>
        transaction.ExecuteAsync(async token =>
        {
            await productEntityService.DeleteAsync(id, token);
            return true;
        }, cancellationToken);

    /// <summary>상태 전이 한 건 — Domain 판단, 변경, 이력. 트랜잭션 안에서만 부른다.</summary>
    private async Task<ProductDto> ChangeStatusCoreAsync(ProductDto product, ProductStatus status, CancellationToken token)
    {
        ProductStatusTransition.EnsureCanChange(product.Status, status);
        var changed = await productEntityService.UpdateStatusAsync(product.Id, status, token);
        await statusHistoryEntityService.CreateAsync(new ProductStatusHistoryCreateDto(product.Id, product.Status, status), token);
        return changed;
    }

    /// <summary>상품이 없거나 볼 수 없으면 404 — 빈 목록으로 "없는 상품" 과 "이력 없는 상품" 을 구분할 수 없게 하지 않는다.</summary>
    public async Task<IReadOnlyList<ProductStatusHistoryDto>> ListStatusHistoryAsync(
        long id, ProductViewerDto viewer, CancellationToken cancellationToken)
    {
        await GetVisibleAsync(id, viewer, cancellationToken);
        return await statusHistoryEntityService.ListByProductAsync(id, cancellationToken);
    }
}
