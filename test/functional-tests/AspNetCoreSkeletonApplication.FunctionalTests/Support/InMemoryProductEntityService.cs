using System.Collections.Concurrent;
using AspNetCoreSkeletonApplication.Common.Dtos;
using AspNetCoreSkeletonApplication.Common.Enums;
using AspNetCoreSkeletonApplication.Common.Errors;
using AspNetCoreSkeletonApplication.Common.Persistence.Services;

namespace AspNetCoreSkeletonApplication.FunctionalTests.Support;

/// <summary>DB 없이 HTTP 파이프라인을 보려고 쓰는 가짜 EntityService. 실제 계약(없으면 ItemNotFoundException)을 따른다.</summary>
public sealed class InMemoryProductEntityService : IProductEntityService
{
    private readonly ConcurrentDictionary<long, ProductDto> _products = new();
    private long _lastId;

    public Task<ProductDto> GetAsync(long id, CancellationToken cancellationToken) =>
        _products.TryGetValue(id, out var product)
            ? Task.FromResult(product)
            : throw new ItemNotFoundException($"상품 {id} 가 없다");

    public Task<ProductDto?> FindAsync(long id, CancellationToken cancellationToken) =>
        Task.FromResult(_products.GetValueOrDefault(id));

    public Task<IReadOnlyList<ProductDto>> ListAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<ProductDto>>(_products.Values.OrderBy(product => product.Id).ToArray());

    public Task<ProductDto> CreateAsync(ProductCreateDto product, ProductStatus status, CancellationToken cancellationToken)
    {
        var id = Interlocked.Increment(ref _lastId);
        var created = new ProductDto(id, product.Name, product.Price, status, 0, DateTimeOffset.UnixEpoch, 0, DateTimeOffset.UnixEpoch);
        _products[id] = created;
        return Task.FromResult(created);
    }

    public async Task<ProductDto> UpdateStatusAsync(long id, ProductStatus status, CancellationToken cancellationToken)
    {
        var updated = await GetAsync(id, cancellationToken) with { Status = status };
        _products[id] = updated;
        return updated;
    }
}
