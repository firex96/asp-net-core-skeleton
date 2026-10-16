using AspNetCoreSkeletonApplication.Common.Product.Services.Biz;
using AspNetCoreSkeletonApplication.Server.Product.Mappers;
using AspNetCoreSkeletonApplication.Server.Product.Models;

namespace AspNetCoreSkeletonApplication.Server.Product.Services.Mapping;

/// <summary>Model ↔ Dto 변환과 공유 <see cref="ProductBizService"/> 호출. 화면용 계산이 생기면 여기에 둔다.</summary>
public class ProductMappingService(ProductBizService bizService)
{
    public async Task<ProductModel> GetAsync(long id, ProductViewerModel viewer, CancellationToken cancellationToken) =>
        (await bizService.GetVisibleAsync(id, viewer.ToDto(), cancellationToken)).ToModel();

    public async Task<IReadOnlyList<ProductModel>> ListAsync(ProductViewerModel viewer, CancellationToken cancellationToken) =>
        (await bizService.ListAsync(viewer.ToDto(), cancellationToken)).ToModels();

    public async Task<ProductModel> CreateAsync(ProductCreateModel model, CancellationToken cancellationToken) =>
        (await bizService.CreateAsync(model.ToDto(), cancellationToken)).ToModel();

    public async Task<ProductModel> ChangeStatusAsync(long id, ProductStatusChangeModel model, CancellationToken cancellationToken) =>
        (await bizService.ChangeStatusAsync(id, model.Status, cancellationToken)).ToModel();

    public Task DeleteAsync(long id, CancellationToken cancellationToken) =>
        bizService.DeleteAsync(id, cancellationToken);

    public async Task<IReadOnlyList<ProductStatusHistoryModel>> ListStatusHistoryAsync(
        long id, ProductViewerModel viewer, CancellationToken cancellationToken) =>
        (await bizService.ListStatusHistoryAsync(id, viewer.ToDto(), cancellationToken)).ToModels();
}
