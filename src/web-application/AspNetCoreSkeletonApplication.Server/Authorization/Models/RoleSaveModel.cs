using System.ComponentModel.DataAnnotations;

namespace AspNetCoreSkeletonApplication.Server.Authorization.Models;

/// <summary>역할 생성 · 수정 공통. 권한은 집합 전체를 보낸다(빠진 코드는 지워진다). 코드는 카탈로그(<c>GET /api/authorization/permissions</c>)에 있어야 한다.</summary>
public sealed record RoleSaveModel(
    [Required, StringLength(100)] string Name,
    [Required(AllowEmptyStrings = true), StringLength(500)] string Description,
    [Required] IReadOnlyList<string> Permissions);
