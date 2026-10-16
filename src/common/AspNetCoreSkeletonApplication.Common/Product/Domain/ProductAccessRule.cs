using AspNetCoreSkeletonApplication.Common.Product.Enums;

namespace AspNetCoreSkeletonApplication.Common.Product.Domain;

/// <summary>
/// 상품 하나에 대한 데이터 의존 판정 — 권한 코드만으로는 정할 수 없는 규칙이다. 권한 보유 여부는 호출자가 판정해 넘긴다.
/// </summary>
public static class ProductAccessRule
{
    /// <summary>
    /// 상품 관리 권한이 있으면 누구 것이든, 없으면 아직 판매 전(Draft)인 자기 상품만 지울 수 있다.
    /// <paramref name="actorId"/> 가 null 이면 사용자가 아닌 주체(클라이언트 토큰)다 — 자기 상품이 없다.
    /// </summary>
    public static bool CanDelete(ProductStatus status, long createdBy, long? actorId, bool canManageProducts) =>
        canManageProducts || (status == ProductStatus.Draft && createdBy == actorId);

    /// <summary>
    /// 판매 전(Draft) 상품은 작성자와 상품 관리 권한 보유자만 본다 — 아직 공개하지 않은 상품이다. 판매 중 · 판매 종료는 누구나 본다.
    /// <paramref name="actorId"/> 가 null 이면 익명이거나 사용자가 아닌 주체다.
    /// </summary>
    public static bool CanView(ProductStatus status, long createdBy, long? actorId, bool canManageProducts) =>
        status != ProductStatus.Draft || canManageProducts || createdBy == actorId;
}
