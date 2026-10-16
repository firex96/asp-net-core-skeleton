using AspNetCoreSkeletonApplication.Common.Authorization.Dtos;
using AspNetCoreSkeletonApplication.Common.Authorization.Persistence.Entities;
using AspNetCoreSkeletonApplication.Common.Authorization.Persistence.Mappers;
using AspNetCoreSkeletonApplication.Common.Errors;
using AspNetCoreSkeletonApplication.Common.Persistence.Caching;
using AspNetCoreSkeletonApplication.Common.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace AspNetCoreSkeletonApplication.Common.Authorization.Persistence.Services;

/// <summary>사용자별 소속 조회는 <see cref="EntityCache"/> 를 거치고, 모든 변경은 <c>user_group_member</c> 태그를 무효화한다.</summary>
internal sealed class UserGroupMemberEntityService(AppDbContext dbContext, EntityCache cache) : IUserGroupMemberEntityService
{
    private const string CacheTag = "user_group_member";

    public async Task<IReadOnlyList<UserGroupMemberDto>> ListByGroupAsync(long userGroupId, CancellationToken cancellationToken) =>
        await dbContext.UserGroupMembers.Where(member => member.UserGroupId == userGroupId)
            .OrderBy(member => member.UserId).ProjectToDto().ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<long>> ListGroupIdsOfUserAsync(long userId, CancellationToken cancellationToken) =>
        await cache.FindOrLoadAsync<IReadOnlyList<long>>(
            $"{CacheTag}:list:user:{userId}",
            CacheTag,
            async token => await dbContext.UserGroupMembers.Where(member => member.UserId == userId)
                .Select(member => member.UserGroupId).ToListAsync(token),
            cancellationToken) ?? [];

    public async Task AddAsync(long userGroupId, long userId, CancellationToken cancellationToken)
    {
        if (await dbContext.UserGroupMembers.AnyAsync(member => member.UserGroupId == userGroupId && member.UserId == userId, cancellationToken))
        {
            return;
        }

        dbContext.UserGroupMembers.Add(new UserGroupMemberEntity { UserGroupId = userGroupId, UserId = userId });
        await dbContext.SaveChangesAsync(cancellationToken);
        await cache.InvalidateAsync(CacheTag, cancellationToken);
    }

    public async Task RemoveAsync(long userGroupId, long userId, CancellationToken cancellationToken)
    {
        var entity = await dbContext.UserGroupMembers.AsTracking()
                         .SingleOrDefaultAsync(member => member.UserGroupId == userGroupId && member.UserId == userId, cancellationToken)
                     ?? throw new ItemNotFoundException($"사용자 {userId} 는 그룹 {userGroupId} 에 속해 있지 않다");
        dbContext.UserGroupMembers.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        await cache.InvalidateAsync(CacheTag, cancellationToken);
    }

    public async Task RemoveAllOfGroupAsync(long userGroupId, CancellationToken cancellationToken)
    {
        dbContext.UserGroupMembers.RemoveRange(
            await dbContext.UserGroupMembers.AsTracking().Where(member => member.UserGroupId == userGroupId).ToListAsync(cancellationToken));
        await dbContext.SaveChangesAsync(cancellationToken);
        await cache.InvalidateAsync(CacheTag, cancellationToken);
    }

    public Task<int> CountUsersInGroupsAsync(IReadOnlyCollection<long> userGroupIds, CancellationToken cancellationToken) =>
        dbContext.UserGroupMembers.Where(member => userGroupIds.Contains(member.UserGroupId))
            .Select(member => member.UserId).Distinct().CountAsync(cancellationToken);
}
