using AspNetCoreSkeletonApplication.Common.Enums;

namespace AspNetCoreSkeletonApplication.InternalApi.Models;

/// <summary>내부 호출자에게는 감사 주체 ID 까지 내보낸다 — 같은 Dto 를 앱마다 다른 Model 로 노출하는 예.</summary>
public sealed record ProductModel(
    long Id,
    string Name,
    decimal Price,
    ProductStatus Status,
    long CreatedBy,
    DateTimeOffset CreatedAt,
    long UpdatedBy,
    DateTimeOffset UpdatedAt);
