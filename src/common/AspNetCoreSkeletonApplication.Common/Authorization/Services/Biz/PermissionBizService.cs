using AspNetCoreSkeletonApplication.Common.Authorization.Domain;
using AspNetCoreSkeletonApplication.Common.Authorization.Dtos;
using AspNetCoreSkeletonApplication.Common.Authorization.Persistence.Services;
using AspNetCoreSkeletonApplication.Common.Errors;

namespace AspNetCoreSkeletonApplication.Common.Authorization.Services.Biz;

/// <summary>
/// 그룹 · 역할 · 권한으로 판정한다. 조회 세 가지는 모두 캐시되고, 그룹 · 역할 · 권한이 바뀌면 각 태그가 무효화된다 —
/// 백플레인이 다른 인스턴스에도 곧바로 전하므로 다음 요청부터 바뀐 권한으로 판정한다.
/// </summary>
public class PermissionBizService(
    IUserGroupMemberEntityService memberEntityService,
    IUserGroupRoleEntityService groupRoleEntityService,
    IRolePermissionEntityService rolePermissionEntityService) : IPermissionEvaluator
{
    public IReadOnlyList<PermissionDto> ListCatalog() => Permissions.All;

    public async Task<IReadOnlySet<string>> ListPermissionsAsync(long userId, CancellationToken cancellationToken) =>
        EffectivePermissions.Resolve(
            await memberEntityService.ListGroupIdsOfUserAsync(userId, cancellationToken),
            await groupRoleEntityService.ListAsync(cancellationToken),
            await rolePermissionEntityService.ListAsync(cancellationToken));

    public async Task<bool> HasPermissionAsync(long userId, string permission, CancellationToken cancellationToken) =>
        (await ListPermissionsAsync(userId, cancellationToken)).Contains(permission);

    /// <summary>
    /// 권한 관리 권한을 가진 사용자가 남는지 확인한다 — 마지막 관리자를 없애는 변경은 409 로 되돌린다(운영자가 스스로를 잠그지 않게).
    /// 변경 직후 같은 트랜잭션 안에서 부른다. 캐시를 거치지 않으므로 커밋 전 변경까지 본다.
    /// </summary>
    public async Task EnsureAuthorizationManagerRemainsAsync(CancellationToken cancellationToken)
    {
        var roleIds = await rolePermissionEntityService.ListRoleIdsWithPermissionAsync(Permissions.Authorization.Manage, cancellationToken);
        var groupIds = await groupRoleEntityService.ListGroupIdsWithRolesAsync(roleIds, cancellationToken);
        if (await memberEntityService.CountUsersInGroupsAsync(groupIds, cancellationToken) == 0)
        {
            throw new ConflictException($"'{Permissions.Authorization.Manage}' 권한을 가진 사용자가 한 명은 남아야 한다");
        }
    }
}
