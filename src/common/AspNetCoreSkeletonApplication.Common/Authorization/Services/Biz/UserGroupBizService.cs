using AspNetCoreSkeletonApplication.Common.Account.Dtos;
using AspNetCoreSkeletonApplication.Common.Account.Services.Biz;
using AspNetCoreSkeletonApplication.Common.Authorization.Dtos;
using AspNetCoreSkeletonApplication.Common.Authorization.Persistence.Services;
using AspNetCoreSkeletonApplication.Common.Persistence;

namespace AspNetCoreSkeletonApplication.Common.Authorization.Services.Biz;

/// <summary>
/// 그룹과 그 소속 · 역할. 그룹 · 소속 · 역할 연결의 변경은 이 BizService 와 <see cref="RoleBizService"/> 에서만 일어난다 —
/// 판정 방식을 ReBAC 으로 바꿀 때 관계를 동기화할 자리가 여기다. 권한을 줄이는 변경은 마지막 관리자가 남는지 확인한다.
/// </summary>
public class UserGroupBizService(
    IUserGroupEntityService groupEntityService,
    IUserGroupMemberEntityService memberEntityService,
    IUserGroupRoleEntityService groupRoleEntityService,
    IRoleEntityService roleEntityService,
    PermissionBizService permissionBizService,
    UserAccountBizService userAccountBizService,
    IMainTransaction transaction)
{
    public Task<IReadOnlyList<UserGroupDto>> ListAsync(CancellationToken cancellationToken) =>
        groupEntityService.ListAsync(cancellationToken);

    public Task<UserGroupDto> GetAsync(long id, CancellationToken cancellationToken) =>
        groupEntityService.GetAsync(id, cancellationToken);

    public Task<UserGroupDto> CreateAsync(UserGroupSaveDto group, CancellationToken cancellationToken) =>
        transaction.ExecuteAsync(token => groupEntityService.CreateAsync(group, token), cancellationToken);

    public Task<UserGroupDto> UpdateAsync(long id, UserGroupSaveDto group, CancellationToken cancellationToken) =>
        transaction.ExecuteAsync(token => groupEntityService.UpdateAsync(id, group, token), cancellationToken);

    public Task DeleteAsync(long id, CancellationToken cancellationToken) =>
        transaction.ExecuteAsync(async token =>
        {
            await groupEntityService.GetAsync(id, token);
            await memberEntityService.RemoveAllOfGroupAsync(id, token);
            await groupRoleEntityService.RemoveAllOfGroupAsync(id, token);
            await groupEntityService.DeleteAsync(id, token);
            await permissionBizService.EnsureAuthorizationManagerRemainsAsync(token);
            return true;
        }, cancellationToken);

    public async Task<IReadOnlyList<UserAccountDto>> ListMembersAsync(long id, CancellationToken cancellationToken)
    {
        await groupEntityService.GetAsync(id, cancellationToken);
        var members = await memberEntityService.ListByGroupAsync(id, cancellationToken);
        return await userAccountBizService.ListByIdsAsync(members.Select(member => member.UserId).ToArray(), cancellationToken);
    }

    public Task AddMemberAsync(long id, long userId, CancellationToken cancellationToken) =>
        transaction.ExecuteAsync(async token =>
        {
            await groupEntityService.GetAsync(id, token);
            await userAccountBizService.GetAsync(userId, token);
            await memberEntityService.AddAsync(id, userId, token);
            return true;
        }, cancellationToken);

    public Task RemoveMemberAsync(long id, long userId, CancellationToken cancellationToken) =>
        transaction.ExecuteAsync(async token =>
        {
            await memberEntityService.RemoveAsync(id, userId, token);
            await permissionBizService.EnsureAuthorizationManagerRemainsAsync(token);
            return true;
        }, cancellationToken);

    public async Task<IReadOnlyList<long>> ListRoleIdsAsync(long id, CancellationToken cancellationToken)
    {
        await groupEntityService.GetAsync(id, cancellationToken);
        return (await groupRoleEntityService.ListAsync(cancellationToken))
            .Where(groupRole => groupRole.UserGroupId == id)
            .Select(groupRole => groupRole.RoleId)
            .ToArray();
    }

    public Task AssignRoleAsync(long id, long roleId, CancellationToken cancellationToken) =>
        transaction.ExecuteAsync(async token =>
        {
            await groupEntityService.GetAsync(id, token);
            await roleEntityService.GetAsync(roleId, token);
            await groupRoleEntityService.AddAsync(id, roleId, token);
            return true;
        }, cancellationToken);

    public Task UnassignRoleAsync(long id, long roleId, CancellationToken cancellationToken) =>
        transaction.ExecuteAsync(async token =>
        {
            await groupRoleEntityService.RemoveAsync(id, roleId, token);
            await permissionBizService.EnsureAuthorizationManagerRemainsAsync(token);
            return true;
        }, cancellationToken);
}
