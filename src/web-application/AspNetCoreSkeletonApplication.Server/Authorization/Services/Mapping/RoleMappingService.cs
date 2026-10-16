using AspNetCoreSkeletonApplication.Common.Authorization.Dtos;
using AspNetCoreSkeletonApplication.Common.Authorization.Services.Biz;
using AspNetCoreSkeletonApplication.Server.Authorization.Mappers;
using AspNetCoreSkeletonApplication.Server.Authorization.Models;

namespace AspNetCoreSkeletonApplication.Server.Authorization.Services.Mapping;

/// <summary>역할과 역할별 권한 목록(두 BizService 결과)을 한 Model 로 합친다.</summary>
public class RoleMappingService(RoleBizService roleBizService)
{
    public async Task<IReadOnlyList<RoleModel>> ListAsync(CancellationToken cancellationToken)
    {
        var roles = await roleBizService.ListAsync(cancellationToken);
        var permissions = await roleBizService.ListPermissionsAsync(cancellationToken);
        return roles.Select(role => role.ToModel(PermissionsOf(role.Id, permissions))).ToArray();
    }

    public async Task<RoleModel> GetAsync(long id, CancellationToken cancellationToken) =>
        await WithPermissionsAsync(await roleBizService.GetAsync(id, cancellationToken), cancellationToken);

    public async Task<RoleModel> CreateAsync(RoleSaveModel model, CancellationToken cancellationToken) =>
        await WithPermissionsAsync(await roleBizService.CreateAsync(model.ToDto(), model.Permissions, cancellationToken), cancellationToken);

    public async Task<RoleModel> UpdateAsync(long id, RoleSaveModel model, CancellationToken cancellationToken) =>
        await WithPermissionsAsync(await roleBizService.UpdateAsync(id, model.ToDto(), model.Permissions, cancellationToken), cancellationToken);

    public Task DeleteAsync(long id, CancellationToken cancellationToken) =>
        roleBizService.DeleteAsync(id, cancellationToken);

    private async Task<RoleModel> WithPermissionsAsync(RoleDto role, CancellationToken cancellationToken) =>
        role.ToModel(PermissionsOf(role.Id, await roleBizService.ListPermissionsAsync(cancellationToken)));

    /// <summary>역할 하나의 권한 코드(정렬). 그룹의 역할 목록도 같은 모양으로 만든다.</summary>
    internal static IReadOnlyList<string> PermissionsOf(long roleId, IReadOnlyList<RolePermissionDto> permissions) =>
        permissions.Where(permission => permission.RoleId == roleId).Select(permission => permission.Permission).Order(StringComparer.Ordinal).ToArray();
}
