using AspNetCoreSkeletonApplication.Common.Authorization;
using AspNetCoreSkeletonApplication.Common.Web.Authorization;
using AspNetCoreSkeletonApplication.Server.Authorization.Models;
using AspNetCoreSkeletonApplication.Server.Authorization.Services.Mapping;
using Microsoft.AspNetCore.Mvc;

namespace AspNetCoreSkeletonApplication.Server.Authorization.Controllers;

/// <summary>권한 코드 카탈로그 — 역할에 붙일 수 있는 코드 목록.</summary>
[ApiController]
[Route("api/authorization/permissions")]
[HasPermission(Permissions.Authorization.Manage)]
public class PermissionController(PermissionMappingService mappingService) : ControllerBase
{
    [HttpGet]
    public IReadOnlyList<PermissionModel> List() => mappingService.ListCatalog();
}
