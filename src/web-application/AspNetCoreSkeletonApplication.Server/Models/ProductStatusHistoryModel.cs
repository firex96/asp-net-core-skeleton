using AspNetCoreSkeletonApplication.Common.Enums;

namespace AspNetCoreSkeletonApplication.Server.Models;

public sealed record ProductStatusHistoryModel(
    long Id,
    ProductStatus FromStatus,
    ProductStatus ToStatus,
    DateTimeOffset CreatedAt);
