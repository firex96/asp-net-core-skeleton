using AspNetCoreSkeletonApplication.Common.Authorization.Dtos;
using AspNetCoreSkeletonApplication.Common.Authorization.Persistence.Entities;
using AspNetCoreSkeletonApplication.Common.Authorization.Persistence.Mappers;
using AspNetCoreSkeletonApplication.Common.Errors;
using AspNetCoreSkeletonApplication.Common.Persistence.Caching;
using AspNetCoreSkeletonApplication.Common.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace AspNetCoreSkeletonApplication.Common.Authorization.Persistence.Services;

/// <summary>조회는 <see cref="EntityCache"/> 를 거치고, 모든 변경은 <c>user_group</c> 태그를 무효화한다.</summary>
internal sealed class UserGroupEntityService(AppDbContext dbContext, EntityCache cache) : IUserGroupEntityService
{
    private const string CacheTag = "user_group";

    public async Task<UserGroupDto> GetAsync(long id, CancellationToken cancellationToken) =>
        await cache.FindOrLoadAsync(
            $"{CacheTag}:{id}",
            CacheTag,
            async token => await dbContext.UserGroups.Where(group => group.Id == id).ProjectToDto().SingleOrDefaultAsync(token),
            cancellationToken)
        ?? throw NotFound(id);

    public async Task<IReadOnlyList<UserGroupDto>> ListAsync(CancellationToken cancellationToken) =>
        await cache.FindOrLoadAsync<IReadOnlyList<UserGroupDto>>(
            $"{CacheTag}:list",
            CacheTag,
            async token => await dbContext.UserGroups.OrderBy(group => group.Name).ProjectToDto().ToListAsync(token),
            cancellationToken) ?? [];

    public async Task<UserGroupDto> CreateAsync(UserGroupSaveDto group, CancellationToken cancellationToken)
    {
        await EnsureNameIsFreeAsync(group.Name, exceptId: null, cancellationToken);
        var entity = group.ToEntity();
        dbContext.UserGroups.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        await cache.InvalidateAsync(CacheTag, cancellationToken);
        return entity.ToDto();
    }

    public async Task<UserGroupDto> UpdateAsync(long id, UserGroupSaveDto group, CancellationToken cancellationToken)
    {
        await EnsureNameIsFreeAsync(group.Name, exceptId: id, cancellationToken);
        var entity = await TrackAsync(id, cancellationToken);
        entity.Name = group.Name;
        entity.Description = group.Description;
        await dbContext.SaveChangesAsync(cancellationToken);
        await cache.InvalidateAsync(CacheTag, cancellationToken);
        return entity.ToDto();
    }

    public async Task DeleteAsync(long id, CancellationToken cancellationToken)
    {
        dbContext.UserGroups.Remove(await TrackAsync(id, cancellationToken));
        await dbContext.SaveChangesAsync(cancellationToken);
        await cache.InvalidateAsync(CacheTag, cancellationToken);
    }

    private async Task EnsureNameIsFreeAsync(string name, long? exceptId, CancellationToken cancellationToken)
    {
        if (await dbContext.UserGroups.AnyAsync(group => group.Name == name && group.Id != exceptId, cancellationToken))
        {
            throw new ConflictException($"그룹 이름 '{name}' 이 이미 있다");
        }
    }

    private async Task<UserGroupEntity> TrackAsync(long id, CancellationToken cancellationToken) =>
        await dbContext.UserGroups.AsTracking().SingleOrDefaultAsync(group => group.Id == id, cancellationToken)
        ?? throw NotFound(id);

    private static ItemNotFoundException NotFound(long id) => new($"그룹 {id} 가 없다");
}
