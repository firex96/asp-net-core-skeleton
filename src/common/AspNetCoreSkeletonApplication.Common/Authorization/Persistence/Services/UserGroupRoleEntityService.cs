using AspNetCoreSkeletonApplication.Common.Authorization.Dtos;
using AspNetCoreSkeletonApplication.Common.Authorization.Persistence.Entities;
using AspNetCoreSkeletonApplication.Common.Authorization.Persistence.Mappers;
using AspNetCoreSkeletonApplication.Common.Errors;
using AspNetCoreSkeletonApplication.Common.Persistence.Caching;
using AspNetCoreSkeletonApplication.Common.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace AspNetCoreSkeletonApplication.Common.Authorization.Persistence.Services;

/// <summary>전체 목록은 <see cref="EntityCache"/> 를 거치고, 모든 변경은 <c>user_group_role</c> 태그를 무효화한다.</summary>
internal sealed class UserGroupRoleEntityService(AppDbContext dbContext, EntityCache cache) : IUserGroupRoleEntityService
{
    private const string CacheTag = "user_group_role";

    public async Task<IReadOnlyList<UserGroupRoleDto>> ListAsync(CancellationToken cancellationToken) =>
        await cache.FindOrLoadAsync<IReadOnlyList<UserGroupRoleDto>>(
            $"{CacheTag}:list",
            CacheTag,
            async token => await dbContext.UserGroupRoles.OrderBy(groupRole => groupRole.Id).ProjectToDto().ToListAsync(token),
            cancellationToken) ?? [];

    public async Task AddAsync(long userGroupId, long roleId, CancellationToken cancellationToken)
    {
        if (await dbContext.UserGroupRoles.AnyAsync(groupRole => groupRole.UserGroupId == userGroupId && groupRole.RoleId == roleId, cancellationToken))
        {
            return;
        }

        dbContext.UserGroupRoles.Add(new UserGroupRoleEntity { UserGroupId = userGroupId, RoleId = roleId });
        await dbContext.SaveChangesAsync(cancellationToken);
        await cache.InvalidateAsync(CacheTag, cancellationToken);
    }

    public async Task RemoveAsync(long userGroupId, long roleId, CancellationToken cancellationToken)
    {
        var entity = await dbContext.UserGroupRoles.AsTracking()
                         .SingleOrDefaultAsync(groupRole => groupRole.UserGroupId == userGroupId && groupRole.RoleId == roleId, cancellationToken)
                     ?? throw new ItemNotFoundException($"그룹 {userGroupId} 에 역할 {roleId} 가 없다");
        dbContext.UserGroupRoles.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        await cache.InvalidateAsync(CacheTag, cancellationToken);
    }

    public Task RemoveAllOfGroupAsync(long userGroupId, CancellationToken cancellationToken) =>
        RemoveWhereAsync(groupRole => groupRole.UserGroupId == userGroupId, cancellationToken);

    public Task RemoveAllOfRoleAsync(long roleId, CancellationToken cancellationToken) =>
        RemoveWhereAsync(groupRole => groupRole.RoleId == roleId, cancellationToken);

    public async Task<IReadOnlyList<long>> ListGroupIdsWithRolesAsync(IReadOnlyCollection<long> roleIds, CancellationToken cancellationToken) =>
        await dbContext.UserGroupRoles.Where(groupRole => roleIds.Contains(groupRole.RoleId))
            .Select(groupRole => groupRole.UserGroupId).Distinct().ToListAsync(cancellationToken);

    private async Task RemoveWhereAsync(System.Linq.Expressions.Expression<Func<UserGroupRoleEntity, bool>> predicate, CancellationToken cancellationToken)
    {
        dbContext.UserGroupRoles.RemoveRange(await dbContext.UserGroupRoles.AsTracking().Where(predicate).ToListAsync(cancellationToken));
        await dbContext.SaveChangesAsync(cancellationToken);
        await cache.InvalidateAsync(CacheTag, cancellationToken);
    }
}
