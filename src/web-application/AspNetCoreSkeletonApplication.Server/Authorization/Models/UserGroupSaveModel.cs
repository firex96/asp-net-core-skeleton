using System.ComponentModel.DataAnnotations;

namespace AspNetCoreSkeletonApplication.Server.Authorization.Models;

/// <summary>그룹 생성 · 수정 공통.</summary>
public sealed record UserGroupSaveModel(
    [Required, StringLength(100)] string Name,
    [Required(AllowEmptyStrings = true), StringLength(500)] string Description);
