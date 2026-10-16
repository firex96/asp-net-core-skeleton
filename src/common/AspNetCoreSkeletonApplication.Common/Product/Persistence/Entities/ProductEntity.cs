using AspNetCoreSkeletonApplication.Common.Product.Enums;
using AspNetCoreSkeletonApplication.Common.Persistence.Entities.Backbone;

namespace AspNetCoreSkeletonApplication.Common.Product.Persistence.Entities;

internal sealed class ProductEntity : BaseAuditedEntity, ISoftDeletable
{
    public required string Name { get; set; }

    public decimal Price { get; set; }

    public ProductStatus Status { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public long? DeletedBy { get; set; }
}
