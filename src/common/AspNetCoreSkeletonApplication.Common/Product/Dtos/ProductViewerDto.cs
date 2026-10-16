namespace AspNetCoreSkeletonApplication.Common.Product.Dtos;

/// <summary>상품을 보는 사람 — 사용자 ID(익명 · 클라이언트 토큰이면 null)와 상품 관리 권한(요청 단위로 판정한 값).</summary>
public sealed record ProductViewerDto(long? ActorId, bool CanManageProducts);
