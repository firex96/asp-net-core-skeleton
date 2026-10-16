namespace AspNetCoreSkeletonApplication.Common.Authorization.Dtos;

/// <summary>그룹 생성 · 수정 공통.</summary>
public sealed record UserGroupSaveDto(string Name, string Description);
