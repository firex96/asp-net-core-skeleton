using AspNetCoreSkeletonApplication.Common.Dtos;
using AspNetCoreSkeletonApplication.Common.Enums;
using AspNetCoreSkeletonApplication.Common.Errors;
using AspNetCoreSkeletonApplication.Common.Persistence.Caching;
using AspNetCoreSkeletonApplication.Common.Persistence.Context;
using AspNetCoreSkeletonApplication.Common.Persistence.Entities.Main;
using AspNetCoreSkeletonApplication.Common.Persistence.Mappers;
using Microsoft.EntityFrameworkCore;

namespace AspNetCoreSkeletonApplication.Common.Persistence.Services;

/// <summary>
/// internal 이다 — 밖에서는 <see cref="IProductEntityService"/> 로만 쓴다. 조회는 기본 NoTracking 이고,
/// 바꿀 때만 <c>AsTracking()</c> 으로 읽는다. 조회는 <see cref="EntityCache"/> 를 거치고, 모든 변경은 <c>product</c> 태그를 무효화한다.
/// </summary>
internal sealed class ProductEntityService(AppDbContext dbContext, EntityCache cache) : IProductEntityService
{
    private const string CacheTag = "product";

    public async Task<ProductDto> GetAsync(long id, CancellationToken cancellationToken) =>
        await FindAsync(id, cancellationToken) ?? throw NotFound(id);

    public Task<ProductDto?> FindAsync(long id, CancellationToken cancellationToken) =>
        cache.FindOrLoadAsync(
            $"{CacheTag}:{id}",
            CacheTag,
            async token => await dbContext.Products.Where(product => product.Id == id).ProjectToDto().SingleOrDefaultAsync(token),
            cancellationToken);

    public async Task<IReadOnlyList<ProductDto>> ListAsync(CancellationToken cancellationToken) =>
        await cache.FindOrLoadAsync<IReadOnlyList<ProductDto>>(
            $"{CacheTag}:list",
            CacheTag,
            async token => await dbContext.Products.OrderBy(product => product.Id).ProjectToDto().ToListAsync(token),
            cancellationToken) ?? [];

    public async Task<IReadOnlyList<ProductDto>> ListStaleDraftsAsync(
        DateTimeOffset updatedBefore, long afterId, int pageSize, CancellationToken cancellationToken) =>
        await dbContext.Products
            .Where(product => product.Status == ProductStatus.Draft && product.UpdatedAt < updatedBefore && product.Id > afterId)
            .OrderBy(product => product.Id)
            .Take(pageSize)
            .ProjectToDto()
            .ToListAsync(cancellationToken);

    public async Task<ProductDto> CreateAsync(ProductCreateDto product, ProductStatus status, CancellationToken cancellationToken)
    {
        var entity = product.ToEntity(status);
        dbContext.Products.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        await cache.InvalidateAsync(CacheTag, cancellationToken);
        return entity.ToDto();
    }

    public async Task<ProductDto> UpdateStatusAsync(long id, ProductStatus status, CancellationToken cancellationToken)
    {
        var entity = await TrackAsync(id, cancellationToken);
        entity.Status = status;
        await dbContext.SaveChangesAsync(cancellationToken);
        await cache.InvalidateAsync(CacheTag, cancellationToken);
        return entity.ToDto();
    }

    public async Task DeleteAsync(long id, CancellationToken cancellationToken)
    {
        dbContext.Products.Remove(await TrackAsync(id, cancellationToken));
        await dbContext.SaveChangesAsync(cancellationToken);
        await cache.InvalidateAsync(CacheTag, cancellationToken);
    }

    private async Task<ProductEntity> TrackAsync(long id, CancellationToken cancellationToken) =>
        await dbContext.Products.AsTracking().SingleOrDefaultAsync(product => product.Id == id, cancellationToken)
        ?? throw NotFound(id);

    private static ItemNotFoundException NotFound(long id) => new($"상품 {id} 가 없다");
}
