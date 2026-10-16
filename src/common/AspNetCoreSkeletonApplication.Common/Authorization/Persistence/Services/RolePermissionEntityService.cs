using AspNetCoreSkeletonApplication.Common.Authorization.Dtos;
using AspNetCoreSkeletonApplication.Common.Authorization.Persistence.Entities;
using AspNetCoreSkeletonApplication.Common.Authorization.Persistence.Mappers;
using AspNetCoreSkeletonApplication.Common.Errors;
using AspNetCoreSkeletonApplication.Common.Persistence.Caching;
using AspNetCoreSkeletonApplication.Common.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace AspNetCoreSkeletonApplication.Common.Authorization.Persistence.Services;

/// <summary>전체 목록은 <see cref="EntityCache"/> 를 거치고, 모든 변경은 <c>role_permission</c> 태그를 무효화한다.</summary>
internal sealed class RolePermissionEntityService(AppDbContext dbContext, EntityCache cache) : IRolePermissionEntityService
{
    private const string CacheTag = "role_permission";

    public async Task<IReadOnlyList<RolePermissionDto>> ListAsync(CancellationToken cancellationToken) =>
        await cache.FindOrLoadAsync<IReadOnlyList<RolePermissionDto>>(
            $"{CacheTag}:list",
            CacheTag,
            async token => await dbContext.RolePermissions.OrderBy(rolePermission => rolePermission.Id).ProjectToDto().ToListAsync(token),
            cancellationToken) ?? [];

    public async Task ReplaceAsync(long roleId, IReadOnlyCollection<string> permissions, CancellationToken cancellationToken)
    {
        var current = await dbContext.RolePermissions.AsTracking().Where(rolePermission => rolePermission.RoleId == roleId).ToListAsync(cancellationToken);
        dbContext.RolePermissions.RemoveRange(current.Where(rolePermission => !permissions.Contains(rolePermission.Permission)));
        dbContext.RolePermissions.AddRange(permissions
            .Where(permission => current.All(rolePermission => rolePermission.Permission != permission))
            .Select(permission => new RolePermissionEntity { RoleId = roleId, Permission = permission }));
        await dbContext.SaveChangesAsync(cancellationToken);
        await cache.InvalidateAsync(CacheTag, cancellationToken);
    }

    public async Task<IReadOnlyList<long>> ListRoleIdsWithPermissionAsync(string permission, CancellationToken cancellationToken) =>
        await dbContext.RolePermissions.Where(rolePermission => rolePermission.Permission == permission)
            .Select(rolePermission => rolePermission.RoleId).Distinct().ToListAsync(cancellationToken);
}
