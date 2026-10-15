namespace AspNetCoreSkeletonApplication.Common.Persistence.Entities.Backbone;

/// <summary>모든 엔티티의 기반 — <c>BIGINT</c> 식별자 하나. DB 가 생성한다(IDENTITY).</summary>
internal abstract class BaseEntity
{
    public long Id { get; set; }
}
