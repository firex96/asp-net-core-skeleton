namespace AspNetCoreSkeletonApplication.Common.Persistence.Entities.Backbone;

/// <summary>
/// 추가 전용(append-only) 엔티티 — 이력 · 로그처럼 한 번 쓰고 바꾸지 않는 행. 생성 주체와 시각만 갖고,
/// <c>AuditSaveChangesInterceptor</c> 가 채운다. 수정 · 삭제하면 저장할 때 예외가 난다.
/// </summary>
internal abstract class BaseCreateAuditedEntity : BaseEntity
{
    public long CreatedBy { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
