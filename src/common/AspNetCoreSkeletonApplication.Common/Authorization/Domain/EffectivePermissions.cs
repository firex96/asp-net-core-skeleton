using AspNetCoreSkeletonApplication.Common.Authorization.Dtos;

namespace AspNetCoreSkeletonApplication.Common.Authorization.Domain;

/// <summary>사용자의 실효 권한 — 속한 그룹 → 그룹의 역할 → 역할의 권한을 펼친 합집합. 사용자에게 직접 붙는 권한은 없다.</summary>
public static class EffectivePermissions
{
    public static IReadOnlySet<string> Resolve(
        IReadOnlyCollection<long> userGroupIds,
        IReadOnlyList<UserGroupRoleDto> groupRoles,
        IReadOnlyList<RolePermissionDto> rolePermissions)
    {
        var roleIds = groupRoles.Where(groupRole => userGroupIds.Contains(groupRole.UserGroupId))
            .Select(groupRole => groupRole.RoleId)
            .ToHashSet();
        return rolePermissions.Where(rolePermission => roleIds.Contains(rolePermission.RoleId))
            .Select(rolePermission => rolePermission.Permission)
            .ToHashSet(StringComparer.Ordinal);
    }
}
