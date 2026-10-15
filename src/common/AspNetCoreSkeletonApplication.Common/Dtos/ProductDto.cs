using AspNetCoreSkeletonApplication.Common.Enums;

namespace AspNetCoreSkeletonApplication.Common.Dtos;

public sealed record ProductDto(
    long Id,
    string Name,
    decimal Price,
    ProductStatus Status,
    long CreatedBy,
    DateTimeOffset CreatedAt,
    long UpdatedBy,
    DateTimeOffset UpdatedAt);
