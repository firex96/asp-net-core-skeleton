using AspNetCoreSkeletonApplication.Common.Product.Enums;

namespace AspNetCoreSkeletonApplication.Common.Product.Dtos;

public sealed record ProductStatusHistoryDto(
    long Id,
    long ProductId,
    ProductStatus FromStatus,
    ProductStatus ToStatus,
    long CreatedBy,
    DateTimeOffset CreatedAt);
