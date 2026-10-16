using AspNetCoreSkeletonApplication.Common.Authorization.Dtos;
using AspNetCoreSkeletonApplication.Common.Authorization.Persistence.Entities;
using AspNetCoreSkeletonApplication.Common.Authorization.Persistence.Mappers;
using AspNetCoreSkeletonApplication.Common.Errors;
using AspNetCoreSkeletonApplication.Common.Persistence.Caching;
using AspNetCoreSkeletonApplication.Common.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace AspNetCoreSkeletonApplication.Common.Authorization.Persistence.Services;

/// <summary>조회는 <see cref="EntityCache"/> 를 거치고, 모든 변경은 <c>role</c> 태그를 무효화한다.</summary>
internal sealed class RoleEntityService(AppDbContext dbContext, EntityCache cache) : IRoleEntityService
{
    private const string CacheTag = "role";

    public async Task<RoleDto> GetAsync(long id, CancellationToken cancellationToken) =>
        await cache.FindOrLoadAsync(
            $"{CacheTag}:{id}",
            CacheTag,
            async token => await dbContext.Roles.Where(role => role.Id == id).ProjectToDto().SingleOrDefaultAsync(token),
            cancellationToken)
        ?? throw NotFound(id);

    public async Task<IReadOnlyList<RoleDto>> ListAsync(CancellationToken cancellationToken) =>
        await cache.FindOrLoadAsync<IReadOnlyList<RoleDto>>(
            $"{CacheTag}:list",
            CacheTag,
            async token => await dbContext.Roles.OrderBy(role => role.Name).ProjectToDto().ToListAsync(token),
            cancellationToken) ?? [];

    public async Task<RoleDto> CreateAsync(RoleSaveDto role, CancellationToken cancellationToken)
    {
        await EnsureNameIsFreeAsync(role.Name, exceptId: null, cancellationToken);
        var entity = role.ToEntity();
        dbContext.Roles.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        await cache.InvalidateAsync(CacheTag, cancellationToken);
        return entity.ToDto();
    }

    public async Task<RoleDto> UpdateAsync(long id, RoleSaveDto role, CancellationToken cancellationToken)
    {
        await EnsureNameIsFreeAsync(role.Name, exceptId: id, cancellationToken);
        var entity = await TrackAsync(id, cancellationToken);
        entity.Name = role.Name;
        entity.Description = role.Description;
        await dbContext.SaveChangesAsync(cancellationToken);
        await cache.InvalidateAsync(CacheTag, cancellationToken);
        return entity.ToDto();
    }

    public async Task DeleteAsync(long id, CancellationToken cancellationToken)
    {
        dbContext.Roles.Remove(await TrackAsync(id, cancellationToken));
        await dbContext.SaveChangesAsync(cancellationToken);
        await cache.InvalidateAsync(CacheTag, cancellationToken);
    }

    private async Task EnsureNameIsFreeAsync(string name, long? exceptId, CancellationToken cancellationToken)
    {
        if (await dbContext.Roles.AnyAsync(role => role.Name == name && role.Id != exceptId, cancellationToken))
        {
            throw new ConflictException($"역할 이름 '{name}' 이 이미 있다");
        }
    }

    private async Task<RoleEntity> TrackAsync(long id, CancellationToken cancellationToken) =>
        await dbContext.Roles.AsTracking().SingleOrDefaultAsync(role => role.Id == id, cancellationToken)
        ?? throw NotFound(id);

    private static ItemNotFoundException NotFound(long id) => new($"역할 {id} 가 없다");
}
