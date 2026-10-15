namespace AspNetCoreSkeletonApplication.Common.Persistence.Entities.Backbone;

/// <summary>
/// 생성·수정 주체와 시각을 가진 엔티티. 값은 <c>AuditSaveChangesInterceptor</c> 가 저장 직전에 채운다 —
/// EntityService 나 매퍼가 직접 넣지 않는다.
/// </summary>
internal abstract class BaseAuditedEntity : BaseEntity
{
    public long CreatedBy { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public long UpdatedBy { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
