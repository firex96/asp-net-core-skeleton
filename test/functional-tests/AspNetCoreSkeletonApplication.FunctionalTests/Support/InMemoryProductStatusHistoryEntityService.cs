using System.Collections.Concurrent;
using AspNetCoreSkeletonApplication.Common.Dtos;
using AspNetCoreSkeletonApplication.Common.Persistence.Services;

namespace AspNetCoreSkeletonApplication.FunctionalTests.Support;

/// <summary>DB 없이 HTTP 파이프라인을 보려고 쓰는 가짜 이력 EntityService.</summary>
public sealed class InMemoryProductStatusHistoryEntityService : IProductStatusHistoryEntityService
{
    private readonly ConcurrentQueue<ProductStatusHistoryDto> _histories = new();
    private long _lastId;

    public Task<ProductStatusHistoryDto> CreateAsync(ProductStatusHistoryCreateDto history, CancellationToken cancellationToken)
    {
        var created = new ProductStatusHistoryDto(
            Interlocked.Increment(ref _lastId), history.ProductId, history.FromStatus, history.ToStatus, 0, DateTimeOffset.UnixEpoch);
        _histories.Enqueue(created);
        return Task.FromResult(created);
    }

    public Task<IReadOnlyList<ProductStatusHistoryDto>> ListByProductAsync(long productId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<ProductStatusHistoryDto>>(_histories.Where(history => history.ProductId == productId).ToArray());
}
