using AspNetCoreSkeletonApplication.Common.Persistence.Entities.Backbone;

namespace AspNetCoreSkeletonApplication.Common.Authorization.Persistence.Entities;

/// <summary>역할 — 권한 코드의 묶음.</summary>
internal sealed class RoleEntity : BaseAuditedEntity, ISoftDeletable
{
    public required string Name { get; set; }

    public required string Description { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public long? DeletedBy { get; set; }
}
