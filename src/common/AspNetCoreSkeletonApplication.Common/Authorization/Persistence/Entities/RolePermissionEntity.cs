using AspNetCoreSkeletonApplication.Common.Persistence.Entities.Backbone;

namespace AspNetCoreSkeletonApplication.Common.Authorization.Persistence.Entities;

/// <summary>역할이 가진 권한 코드.</summary>
internal sealed class RolePermissionEntity : BaseAuditedEntity, ISoftDeletable
{
    public long RoleId { get; set; }

    /// <summary><c>Permissions</c> 카탈로그의 코드.</summary>
    public required string Permission { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public long? DeletedBy { get; set; }
}
