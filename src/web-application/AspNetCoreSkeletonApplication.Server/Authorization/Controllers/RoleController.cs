using AspNetCoreSkeletonApplication.Common.Authorization;
using AspNetCoreSkeletonApplication.Common.Web.Authorization;
using AspNetCoreSkeletonApplication.Server.Authorization.Models;
using AspNetCoreSkeletonApplication.Server.Authorization.Services.Mapping;
using Microsoft.AspNetCore.Mvc;

namespace AspNetCoreSkeletonApplication.Server.Authorization.Controllers;

/// <summary>역할과 역할의 권한 관리. 권한 <c>authorization.manage</c>.</summary>
[ApiController]
[Route("api/authorization/roles")]
[HasPermission(Permissions.Authorization.Manage)]
public class RoleController(RoleMappingService mappingService) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<RoleModel>> List(CancellationToken cancellationToken) =>
        mappingService.ListAsync(cancellationToken);

    [HttpGet("{id:long}")]
    public Task<RoleModel> Get(long id, CancellationToken cancellationToken) =>
        mappingService.GetAsync(id, cancellationToken);

    [HttpPost]
    public async Task<ActionResult<RoleModel>> Create(RoleSaveModel model, CancellationToken cancellationToken)
    {
        var created = await mappingService.CreateAsync(model, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:long}")]
    public Task<RoleModel> Update(long id, RoleSaveModel model, CancellationToken cancellationToken) =>
        mappingService.UpdateAsync(id, model, cancellationToken);

    [HttpDelete("{id:long}")]
    public async Task<NoContentResult> Delete(long id, CancellationToken cancellationToken)
    {
        await mappingService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
