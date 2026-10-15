using AspNetCoreSkeletonApplication.Common.Enums;

namespace AspNetCoreSkeletonApplication.Common.Dtos;

public sealed record ProductStatusHistoryCreateDto(long ProductId, ProductStatus FromStatus, ProductStatus ToStatus);
