using AspNetCoreSkeletonApplication.Common.Product.Enums;

namespace AspNetCoreSkeletonApplication.Server.Product.Models;

public sealed record ProductModel(
    long Id,
    string Name,
    decimal Price,
    ProductStatus Status,
    long CreatedBy,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
