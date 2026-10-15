using AspNetCoreSkeletonApplication.Common.Enums;

namespace AspNetCoreSkeletonApplication.Common.Dtos;

public sealed record ProductStatusHistoryDto(
    long Id,
    long ProductId,
    ProductStatus FromStatus,
    ProductStatus ToStatus,
    long CreatedBy,
    DateTimeOffset CreatedAt);
