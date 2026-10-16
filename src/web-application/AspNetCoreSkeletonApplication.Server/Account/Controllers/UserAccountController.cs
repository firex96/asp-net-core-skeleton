using System.ComponentModel.DataAnnotations;
using AspNetCoreSkeletonApplication.Common.Authorization;
using AspNetCoreSkeletonApplication.Common.Web.Authorization;
using AspNetCoreSkeletonApplication.Server.Account.Models;
using AspNetCoreSkeletonApplication.Server.Account.Services.Mapping;
using Microsoft.AspNetCore.Mvc;

namespace AspNetCoreSkeletonApplication.Server.Account.Controllers;

/// <summary>관리 화면의 사용자 찾기 — 그룹에 넣을 사람을 이메일(정확히 일치)로 찾는다. 권한 <c>authorization.manage</c>. 목록 · 검색은 두지 않는다.</summary>
[ApiController]
[Route("api/authorization/users")]
[HasPermission(Permissions.Authorization.Manage)]
public class UserAccountController(UserAccountMappingService mappingService) : ControllerBase
{
    /// <summary>없으면 404 <c>NOT_FOUND_ERROR</c>.</summary>
    [HttpGet]
    public Task<UserAccountModel> GetByEmail([FromQuery, Required, EmailAddress] string email, CancellationToken cancellationToken) =>
        mappingService.GetByEmailAsync(email, cancellationToken);
}
