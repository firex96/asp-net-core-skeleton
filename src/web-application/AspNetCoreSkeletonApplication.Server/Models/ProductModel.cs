using AspNetCoreSkeletonApplication.Common.Enums;

namespace AspNetCoreSkeletonApplication.Server.Models;

public sealed record ProductModel(
    long Id,
    string Name,
    decimal Price,
    ProductStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
