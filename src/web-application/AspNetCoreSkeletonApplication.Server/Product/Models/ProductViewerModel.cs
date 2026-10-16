namespace AspNetCoreSkeletonApplication.Server.Product.Models;

/// <summary>상품을 보는 사람 — 컨트롤러가 요청의 사용자와 상품 관리 권한(권한 정책으로 판정)으로 만든다.</summary>
public sealed record ProductViewerModel(long? ActorId, bool CanManageProducts);
