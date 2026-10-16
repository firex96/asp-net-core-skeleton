using AspNetCoreSkeletonApplication.Common.Product.Enums;

namespace AspNetCoreSkeletonApplication.Common.Product.Dtos;

public sealed record ProductStatusHistoryCreateDto(long ProductId, ProductStatus FromStatus, ProductStatus ToStatus);
