using AspNetCoreSkeletonApplication.Common.Product.Services.Biz;
using AspNetCoreSkeletonApplication.InternalApi.Product.Mappers;
using AspNetCoreSkeletonApplication.InternalApi.Product.Models;

namespace AspNetCoreSkeletonApplication.InternalApi.Product.Services.Mapping;

/// <summary>Server 와 같은 공유 <see cref="ProductBizService"/> 를 이 앱의 Model 로 노출한다.</summary>
public class ProductMappingService(ProductBizService bizService)
{
    public async Task<ProductModel> GetAsync(long id, CancellationToken cancellationToken) =>
        (await bizService.GetAsync(id, cancellationToken)).ToModel();
}
