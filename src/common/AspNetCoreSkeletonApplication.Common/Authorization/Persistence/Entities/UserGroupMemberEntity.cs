using AspNetCoreSkeletonApplication.Common.Persistence.Entities.Backbone;

namespace AspNetCoreSkeletonApplication.Common.Authorization.Persistence.Entities;

/// <summary>그룹 소속. 빼면 논리 삭제된다(업무 테이블에 물리 삭제 경로를 두지 않는다).</summary>
internal sealed class UserGroupMemberEntity : BaseAuditedEntity, ISoftDeletable
{
    public long UserGroupId { get; set; }

    public long UserId { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public long? DeletedBy { get; set; }
}
