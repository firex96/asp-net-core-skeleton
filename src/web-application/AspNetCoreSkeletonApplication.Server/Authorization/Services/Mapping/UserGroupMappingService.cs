using AspNetCoreSkeletonApplication.Common.Authorization.Services.Biz;
using AspNetCoreSkeletonApplication.Server.Authorization.Mappers;
using AspNetCoreSkeletonApplication.Server.Authorization.Models;

namespace AspNetCoreSkeletonApplication.Server.Authorization.Services.Mapping;

public class UserGroupMappingService(UserGroupBizService userGroupBizService, RoleBizService roleBizService)
{
    public async Task<IReadOnlyList<UserGroupModel>> ListAsync(CancellationToken cancellationToken) =>
        (await userGroupBizService.ListAsync(cancellationToken)).ToModels();

    public async Task<UserGroupModel> GetAsync(long id, CancellationToken cancellationToken) =>
        (await userGroupBizService.GetAsync(id, cancellationToken)).ToModel();

    public async Task<UserGroupModel> CreateAsync(UserGroupSaveModel model, CancellationToken cancellationToken) =>
        (await userGroupBizService.CreateAsync(model.ToDto(), cancellationToken)).ToModel();

    public async Task<UserGroupModel> UpdateAsync(long id, UserGroupSaveModel model, CancellationToken cancellationToken) =>
        (await userGroupBizService.UpdateAsync(id, model.ToDto(), cancellationToken)).ToModel();

    public Task DeleteAsync(long id, CancellationToken cancellationToken) =>
        userGroupBizService.DeleteAsync(id, cancellationToken);

    public async Task<IReadOnlyList<UserGroupMemberModel>> ListMembersAsync(long id, CancellationToken cancellationToken) =>
        (await userGroupBizService.ListMembersAsync(id, cancellationToken)).ToMemberModels();

    public Task AddMemberAsync(long id, long userId, CancellationToken cancellationToken) =>
        userGroupBizService.AddMemberAsync(id, userId, cancellationToken);

    public Task RemoveMemberAsync(long id, long userId, CancellationToken cancellationToken) =>
        userGroupBizService.RemoveMemberAsync(id, userId, cancellationToken);

    /// <summary>그룹의 역할을 역할 목록 화면과 같은 모양(권한 포함)으로 돌려준다.</summary>
    public async Task<IReadOnlyList<RoleModel>> ListRolesAsync(long id, CancellationToken cancellationToken)
    {
        var roleIds = await userGroupBizService.ListRoleIdsAsync(id, cancellationToken);
        var permissions = await roleBizService.ListPermissionsAsync(cancellationToken);
        return (await roleBizService.ListAsync(cancellationToken))
            .Where(role => roleIds.Contains(role.Id))
            .Select(role => role.ToModel(RoleMappingService.PermissionsOf(role.Id, permissions)))
            .ToArray();
    }

    public Task AssignRoleAsync(long id, long roleId, CancellationToken cancellationToken) =>
        userGroupBizService.AssignRoleAsync(id, roleId, cancellationToken);

    public Task UnassignRoleAsync(long id, long roleId, CancellationToken cancellationToken) =>
        userGroupBizService.UnassignRoleAsync(id, roleId, cancellationToken);
}
