using AspNetCoreSkeletonApplication.Common.Persistence.Entities.Backbone;

namespace AspNetCoreSkeletonApplication.Common.Authorization.Persistence.Entities;

/// <summary>그룹이 가진 역할.</summary>
internal sealed class UserGroupRoleEntity : BaseAuditedEntity, ISoftDeletable
{
    public long UserGroupId { get; set; }

    public long RoleId { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public long? DeletedBy { get; set; }
}
