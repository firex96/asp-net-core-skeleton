using AspNetCoreSkeletonApplication.Common.Authorization.Dtos;
using AspNetCoreSkeletonApplication.Common.Authorization.Persistence.Services;
using AspNetCoreSkeletonApplication.Common.Errors;
using AspNetCoreSkeletonApplication.Common.Persistence;

namespace AspNetCoreSkeletonApplication.Common.Authorization.Services.Biz;

/// <summary>역할과 그 권한 코드. 권한 코드는 <see cref="Permissions"/> 카탈로그에 있는 것만 받는다.</summary>
public class RoleBizService(
    IRoleEntityService roleEntityService,
    IRolePermissionEntityService rolePermissionEntityService,
    IUserGroupRoleEntityService groupRoleEntityService,
    PermissionBizService permissionBizService,
    IMainTransaction transaction)
{
    public Task<IReadOnlyList<RoleDto>> ListAsync(CancellationToken cancellationToken) =>
        roleEntityService.ListAsync(cancellationToken);

    public Task<RoleDto> GetAsync(long id, CancellationToken cancellationToken) =>
        roleEntityService.GetAsync(id, cancellationToken);

    public Task<IReadOnlyList<RolePermissionDto>> ListPermissionsAsync(CancellationToken cancellationToken) =>
        rolePermissionEntityService.ListAsync(cancellationToken);

    public Task<RoleDto> CreateAsync(RoleSaveDto role, IReadOnlyCollection<string> permissions, CancellationToken cancellationToken) =>
        transaction.ExecuteAsync(async token =>
        {
            EnsureKnown(permissions);
            var created = await roleEntityService.CreateAsync(role, token);
            await rolePermissionEntityService.ReplaceAsync(created.Id, permissions, token);
            return created;
        }, cancellationToken);

    /// <summary>이름 · 설명과 권한 집합을 함께 바꾼다. 권한을 줄여도 마지막 관리자는 남아야 한다.</summary>
    public Task<RoleDto> UpdateAsync(long id, RoleSaveDto role, IReadOnlyCollection<string> permissions, CancellationToken cancellationToken) =>
        transaction.ExecuteAsync(async token =>
        {
            EnsureKnown(permissions);
            var updated = await roleEntityService.UpdateAsync(id, role, token);
            await rolePermissionEntityService.ReplaceAsync(id, permissions, token);
            await permissionBizService.EnsureAuthorizationManagerRemainsAsync(token);
            return updated;
        }, cancellationToken);

    public Task DeleteAsync(long id, CancellationToken cancellationToken) =>
        transaction.ExecuteAsync(async token =>
        {
            await roleEntityService.GetAsync(id, token);
            await groupRoleEntityService.RemoveAllOfRoleAsync(id, token);
            await rolePermissionEntityService.ReplaceAsync(id, [], token);
            await roleEntityService.DeleteAsync(id, token);
            await permissionBizService.EnsureAuthorizationManagerRemainsAsync(token);
            return true;
        }, cancellationToken);

    private static void EnsureKnown(IReadOnlyCollection<string> permissions)
    {
        var unknown = permissions.Where(permission => !Permissions.Exists(permission)).ToArray();
        if (unknown.Length > 0)
        {
            throw new ClientBadRequestException($"알 수 없는 권한 코드: {string.Join(", ", unknown)}");
        }
    }
}
