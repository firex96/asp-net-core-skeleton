using AspNetCoreSkeletonApplication.Common.Persistence.Entities.Backbone;

namespace AspNetCoreSkeletonApplication.Common.Authorization.Persistence.Entities;

/// <summary>사용자 그룹. 사용자는 그룹에 속하고, 그룹이 역할을 갖는다 — 사용자에게 역할을 직접 주지 않는다.</summary>
internal sealed class UserGroupEntity : BaseAuditedEntity, ISoftDeletable
{
    public required string Name { get; set; }

    public required string Description { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public long? DeletedBy { get; set; }
}
