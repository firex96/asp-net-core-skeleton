namespace AspNetCoreSkeletonApplication.Common.Persistence.Entities.Backbone;

/// <summary>
/// 논리 삭제 엔티티. EntityService 는 그냥 <c>Remove</c> 를 부르고, <c>AuditSaveChangesInterceptor</c> 가 그것을
/// 삭제 주체 · 시각을 채운 수정으로 바꾼다. 지워진 행은 <c>AppDbContext</c> 의 전역 쿼리 필터가 모든 조회에서 뺀다.
/// </summary>
internal interface ISoftDeletable
{
    DateTimeOffset? DeletedAt { get; set; }

    long? DeletedBy { get; set; }
}
