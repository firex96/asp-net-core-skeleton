using AspNetCoreSkeletonApplication.Common.Authorization.Dtos;
using AspNetCoreSkeletonApplication.Common.Authorization.Persistence.Entities;
using Riok.Mapperly.Abstractions;

namespace AspNetCoreSkeletonApplication.Common.Authorization.Persistence.Mappers;

/// <summary>
/// 그룹 · 역할 · 소속 · 권한 엔티티 ↔ Dto. 감사 · 삭제 필드는 내보내지 않고(지워진 행은 쿼리 필터가 이미 뺐다),
/// 만들 때는 식별자는 DB 가, 감사 · 삭제 필드는 인터셉터가 채운다.
/// </summary>
[Mapper]
internal static partial class AuthorizationEntityMapper
{
    [MapperIgnoreSource(nameof(UserGroupEntity.CreatedBy))]
    [MapperIgnoreSource(nameof(UserGroupEntity.CreatedAt))]
    [MapperIgnoreSource(nameof(UserGroupEntity.UpdatedBy))]
    [MapperIgnoreSource(nameof(UserGroupEntity.UpdatedAt))]
    [MapperIgnoreSource(nameof(UserGroupEntity.DeletedAt))]
    [MapperIgnoreSource(nameof(UserGroupEntity.DeletedBy))]
    public static partial UserGroupDto ToDto(this UserGroupEntity entity);

    public static partial IQueryable<UserGroupDto> ProjectToDto(this IQueryable<UserGroupEntity> query);

    [MapperIgnoreTarget(nameof(UserGroupEntity.Id))]
    [MapperIgnoreTarget(nameof(UserGroupEntity.CreatedBy))]
    [MapperIgnoreTarget(nameof(UserGroupEntity.CreatedAt))]
    [MapperIgnoreTarget(nameof(UserGroupEntity.UpdatedBy))]
    [MapperIgnoreTarget(nameof(UserGroupEntity.UpdatedAt))]
    [MapperIgnoreTarget(nameof(UserGroupEntity.DeletedAt))]
    [MapperIgnoreTarget(nameof(UserGroupEntity.DeletedBy))]
    public static partial UserGroupEntity ToEntity(this UserGroupSaveDto dto);

    [MapperIgnoreSource(nameof(RoleEntity.CreatedBy))]
    [MapperIgnoreSource(nameof(RoleEntity.CreatedAt))]
    [MapperIgnoreSource(nameof(RoleEntity.UpdatedBy))]
    [MapperIgnoreSource(nameof(RoleEntity.UpdatedAt))]
    [MapperIgnoreSource(nameof(RoleEntity.DeletedAt))]
    [MapperIgnoreSource(nameof(RoleEntity.DeletedBy))]
    public static partial RoleDto ToDto(this RoleEntity entity);

    public static partial IQueryable<RoleDto> ProjectToDto(this IQueryable<RoleEntity> query);

    [MapperIgnoreTarget(nameof(RoleEntity.Id))]
    [MapperIgnoreTarget(nameof(RoleEntity.CreatedBy))]
    [MapperIgnoreTarget(nameof(RoleEntity.CreatedAt))]
    [MapperIgnoreTarget(nameof(RoleEntity.UpdatedBy))]
    [MapperIgnoreTarget(nameof(RoleEntity.UpdatedAt))]
    [MapperIgnoreTarget(nameof(RoleEntity.DeletedAt))]
    [MapperIgnoreTarget(nameof(RoleEntity.DeletedBy))]
    public static partial RoleEntity ToEntity(this RoleSaveDto dto);

    [MapperIgnoreSource(nameof(UserGroupMemberEntity.Id))]
    [MapperIgnoreSource(nameof(UserGroupMemberEntity.CreatedBy))]
    [MapperIgnoreSource(nameof(UserGroupMemberEntity.CreatedAt))]
    [MapperIgnoreSource(nameof(UserGroupMemberEntity.UpdatedBy))]
    [MapperIgnoreSource(nameof(UserGroupMemberEntity.UpdatedAt))]
    [MapperIgnoreSource(nameof(UserGroupMemberEntity.DeletedAt))]
    [MapperIgnoreSource(nameof(UserGroupMemberEntity.DeletedBy))]
    public static partial UserGroupMemberDto ToDto(this UserGroupMemberEntity entity);

    public static partial IQueryable<UserGroupMemberDto> ProjectToDto(this IQueryable<UserGroupMemberEntity> query);

    [MapperIgnoreSource(nameof(UserGroupRoleEntity.Id))]
    [MapperIgnoreSource(nameof(UserGroupRoleEntity.CreatedBy))]
    [MapperIgnoreSource(nameof(UserGroupRoleEntity.CreatedAt))]
    [MapperIgnoreSource(nameof(UserGroupRoleEntity.UpdatedBy))]
    [MapperIgnoreSource(nameof(UserGroupRoleEntity.UpdatedAt))]
    [MapperIgnoreSource(nameof(UserGroupRoleEntity.DeletedAt))]
    [MapperIgnoreSource(nameof(UserGroupRoleEntity.DeletedBy))]
    public static partial UserGroupRoleDto ToDto(this UserGroupRoleEntity entity);

    public static partial IQueryable<UserGroupRoleDto> ProjectToDto(this IQueryable<UserGroupRoleEntity> query);

    [MapperIgnoreSource(nameof(RolePermissionEntity.Id))]
    [MapperIgnoreSource(nameof(RolePermissionEntity.CreatedBy))]
    [MapperIgnoreSource(nameof(RolePermissionEntity.CreatedAt))]
    [MapperIgnoreSource(nameof(RolePermissionEntity.UpdatedBy))]
    [MapperIgnoreSource(nameof(RolePermissionEntity.UpdatedAt))]
    [MapperIgnoreSource(nameof(RolePermissionEntity.DeletedAt))]
    [MapperIgnoreSource(nameof(RolePermissionEntity.DeletedBy))]
    public static partial RolePermissionDto ToDto(this RolePermissionEntity entity);

    public static partial IQueryable<RolePermissionDto> ProjectToDto(this IQueryable<RolePermissionEntity> query);
}
