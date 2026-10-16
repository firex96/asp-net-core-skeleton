using AspNetCoreSkeletonApplication.Common.Product.Enums;

namespace AspNetCoreSkeletonApplication.Common.Product.Dtos;

public sealed record ProductDto(
    long Id,
    string Name,
    decimal Price,
    ProductStatus Status,
    long CreatedBy,
    DateTimeOffset CreatedAt,
    long UpdatedBy,
    DateTimeOffset UpdatedAt);
