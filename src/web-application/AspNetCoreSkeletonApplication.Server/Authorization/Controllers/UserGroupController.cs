using AspNetCoreSkeletonApplication.Common.Authorization;
using AspNetCoreSkeletonApplication.Common.Web.Authorization;
using AspNetCoreSkeletonApplication.Server.Authorization.Models;
using AspNetCoreSkeletonApplication.Server.Authorization.Services.Mapping;
using Microsoft.AspNetCore.Mvc;

namespace AspNetCoreSkeletonApplication.Server.Authorization.Controllers;

/// <summary>사용자 그룹 · 소속 · 그룹의 역할 관리. 권한 <c>authorization.manage</c>. 마지막 관리자를 없애는 변경은 409.</summary>
[ApiController]
[Route("api/authorization/groups")]
[HasPermission(Permissions.Authorization.Manage)]
public class UserGroupController(UserGroupMappingService mappingService) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<UserGroupModel>> List(CancellationToken cancellationToken) =>
        mappingService.ListAsync(cancellationToken);

    [HttpGet("{id:long}")]
    public Task<UserGroupModel> Get(long id, CancellationToken cancellationToken) =>
        mappingService.GetAsync(id, cancellationToken);

    [HttpPost]
    public async Task<ActionResult<UserGroupModel>> Create(UserGroupSaveModel model, CancellationToken cancellationToken)
    {
        var created = await mappingService.CreateAsync(model, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:long}")]
    public Task<UserGroupModel> Update(long id, UserGroupSaveModel model, CancellationToken cancellationToken) =>
        mappingService.UpdateAsync(id, model, cancellationToken);

    [HttpDelete("{id:long}")]
    public async Task<NoContentResult> Delete(long id, CancellationToken cancellationToken)
    {
        await mappingService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:long}/members")]
    public Task<IReadOnlyList<UserGroupMemberModel>> ListMembers(long id, CancellationToken cancellationToken) =>
        mappingService.ListMembersAsync(id, cancellationToken);

    /// <summary>멱등 — 이미 속해 있어도 204.</summary>
    [HttpPut("{id:long}/members/{userId:long}")]
    public async Task<NoContentResult> AddMember(long id, long userId, CancellationToken cancellationToken)
    {
        await mappingService.AddMemberAsync(id, userId, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:long}/members/{userId:long}")]
    public async Task<NoContentResult> RemoveMember(long id, long userId, CancellationToken cancellationToken)
    {
        await mappingService.RemoveMemberAsync(id, userId, cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:long}/roles")]
    public Task<IReadOnlyList<RoleModel>> ListRoles(long id, CancellationToken cancellationToken) =>
        mappingService.ListRolesAsync(id, cancellationToken);

    /// <summary>멱등 — 이미 있어도 204.</summary>
    [HttpPut("{id:long}/roles/{roleId:long}")]
    public async Task<NoContentResult> AssignRole(long id, long roleId, CancellationToken cancellationToken)
    {
        await mappingService.AssignRoleAsync(id, roleId, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:long}/roles/{roleId:long}")]
    public async Task<NoContentResult> UnassignRole(long id, long roleId, CancellationToken cancellationToken)
    {
        await mappingService.UnassignRoleAsync(id, roleId, cancellationToken);
        return NoContent();
    }
}
