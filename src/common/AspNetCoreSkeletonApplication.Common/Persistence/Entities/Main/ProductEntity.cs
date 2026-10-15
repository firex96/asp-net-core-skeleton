using AspNetCoreSkeletonApplication.Common.Enums;
using AspNetCoreSkeletonApplication.Common.Persistence.Entities.Backbone;

namespace AspNetCoreSkeletonApplication.Common.Persistence.Entities.Main;

internal sealed class ProductEntity : BaseAuditedEntity
{
    public required string Name { get; set; }

    public decimal Price { get; set; }

    public ProductStatus Status { get; set; }
}
