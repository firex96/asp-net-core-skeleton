using AspNetCoreSkeletonApplication.Common.Product.Enums;

namespace AspNetCoreSkeletonApplication.Server.Product.Models;

public sealed record ProductStatusHistoryModel(
    long Id,
    ProductStatus FromStatus,
    ProductStatus ToStatus,
    DateTimeOffset CreatedAt);
