using AspNetCoreSkeletonApplication.Common.Account.Dtos;
using AspNetCoreSkeletonApplication.Common.Authorization.Dtos;
using AspNetCoreSkeletonApplication.Server.Authorization.Models;
using Riok.Mapperly.Abstractions;

namespace AspNetCoreSkeletonApplication.Server.Authorization.Mappers;

[Mapper]
public static partial class AuthorizationModelMapper
{
    public static partial UserGroupModel ToModel(this UserGroupDto dto);

    public static partial IReadOnlyList<UserGroupModel> ToModels(this IReadOnlyList<UserGroupDto> dtos);

    public static partial UserGroupSaveDto ToDto(this UserGroupSaveModel model);

    [MapProperty(nameof(UserAccountDto.Id), nameof(UserGroupMemberModel.UserId))]
    public static partial UserGroupMemberModel ToMemberModel(this UserAccountDto dto);

    public static partial IReadOnlyList<UserGroupMemberModel> ToMemberModels(this IReadOnlyList<UserAccountDto> dtos);

    /// <summary>권한 목록은 역할 Dto 에 없다 — MappingService 가 역할 권한 목록에서 골라 넘긴다.</summary>
    public static partial RoleModel ToModel(this RoleDto dto, IReadOnlyList<string> permissions);

    /// <summary>권한은 따로 넘긴다(RoleBizService 가 역할과 권한을 함께 저장한다).</summary>
    [MapperIgnoreSource(nameof(RoleSaveModel.Permissions))]
    public static partial RoleSaveDto ToDto(this RoleSaveModel model);

    public static partial IReadOnlyList<PermissionModel> ToModels(this IReadOnlyList<PermissionDto> dtos);
}
