using AspNetCoreSkeletonApplication.Common.Enums;
using AspNetCoreSkeletonApplication.Common.Persistence.Entities.Backbone;

namespace AspNetCoreSkeletonApplication.Common.Persistence.Entities.Main;

/// <summary>상품 상태가 바뀔 때마다 한 행. 상품은 navigation 없이 <see cref="ProductId"/> 로만 가리킨다.</summary>
internal sealed class ProductStatusHistoryEntity : BaseCreateAuditedEntity
{
    public long ProductId { get; set; }

    public ProductStatus FromStatus { get; set; }

    public ProductStatus ToStatus { get; set; }
}
