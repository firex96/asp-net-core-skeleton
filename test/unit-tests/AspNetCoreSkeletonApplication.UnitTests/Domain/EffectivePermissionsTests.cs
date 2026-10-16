using AspNetCoreSkeletonApplication.Common.Authorization.Domain;
using AspNetCoreSkeletonApplication.Common.Authorization.Dtos;

namespace AspNetCoreSkeletonApplication.UnitTests.Domain;

public class EffectivePermissionsTests
{
    private static readonly UserGroupRoleDto[] GroupRoles = [new(1, 10), new(2, 20), new(3, 10)];

    private static readonly RolePermissionDto[] RolePermissions =
        [new(10, "products.manage"), new(20, "authorization.manage"), new(20, "products.manage"), new(30, "reports.read")];

    [Fact]
    public void Resolve_UnionsPermissionsOfRolesOfTheUsersGroups()
    {
        var permissions = EffectivePermissions.Resolve([1, 2], GroupRoles, RolePermissions);

        Assert.Equal(["authorization.manage", "products.manage"], permissions.Order());
    }

    [Fact]
    public void Resolve_IgnoresRolesNotReachableFromTheUsersGroups()
    {
        var permissions = EffectivePermissions.Resolve([3], GroupRoles, RolePermissions);

        Assert.Equal(["products.manage"], permissions);
    }

    [Fact]
    public void Resolve_UserWithoutGroups_HasNoPermissions()
    {
        Assert.Empty(EffectivePermissions.Resolve([], GroupRoles, RolePermissions));
    }
}
