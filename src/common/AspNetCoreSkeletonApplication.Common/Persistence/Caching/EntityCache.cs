using AspNetCoreSkeletonApplication.Common.Persistence.Context;
using Microsoft.Extensions.Caching.Hybrid;

namespace AspNetCoreSkeletonApplication.Common.Persistence.Caching;

/// <summary>
/// EntityService 가 쓰는 캐시. <see cref="HybridCache"/>(FusionCache — 메모리 L1 + Redis L2 + 백플레인)를 직접 쓰지 않고 이것을 거친다 — 규칙 두 가지를 한 곳에서 지킨다.
/// <list type="number">
/// <item><b>없는 값은 캐시하지 않는다.</b> 조회 결과가 null 이면 캐시에 넣지 않고 null 을 돌려준다.</item>
/// <item><b>무효화는 커밋 뒤에.</b> 트랜잭션 안에서 무효화하면 커밋 전에 다른 요청이 옛 값을 다시 캐시할 수 있다. 그래서 트랜잭션 안의 무효화는
/// 모아 두었다가 <c>MainTransaction</c> 이 커밋한 직후에 실행하고, 롤백되면 버린다. 트랜잭션 밖이면 바로 실행한다.</item>
/// </list>
/// 키는 <c>{엔티티}:{id}</c> · <c>{엔티티}:list</c> · <c>{엔티티}:list:{조건}</c>, 태그는 <c>{엔티티}</c>(테이블 이름) 다. 캐시하는 엔티티의 모든 변경 메서드는 그 태그 전체를 무효화한다.
/// </summary>
internal sealed class EntityCache(HybridCache cache, AppDbContext dbContext)
{
    private readonly HashSet<string> _pendingTags = [];

    public async Task<T?> FindOrLoadAsync<T>(
        string key, string tag, Func<CancellationToken, Task<T?>> load, CancellationToken cancellationToken)
        where T : class
    {
        try
        {
            return await cache.GetOrCreateAsync(
                key,
                load,
                static async (state, token) => await state(token) ?? throw new NotFoundSignal(),
                tags: [tag],
                cancellationToken: cancellationToken);
        }
        catch (NotFoundSignal)
        {
            return null;
        }
    }

    public async Task InvalidateAsync(string tag, CancellationToken cancellationToken)
    {
        if (dbContext.Database.CurrentTransaction is null)
        {
            await cache.RemoveByTagAsync(tag, cancellationToken);
            return;
        }

        _pendingTags.Add(tag);
    }

    /// <summary>커밋 직후 <c>MainTransaction</c> 이 부른다.</summary>
    public async Task FlushPendingAsync(CancellationToken cancellationToken)
    {
        foreach (var tag in _pendingTags)
        {
            await cache.RemoveByTagAsync(tag, cancellationToken);
        }

        _pendingTags.Clear();
    }

    /// <summary>롤백되면 바뀐 것이 없으므로 버린다.</summary>
    public void DiscardPending() => _pendingTags.Clear();

    /// <summary>"없음" 을 캐시에 넣지 않으려고 factory 를 빠져나가는 신호. 밖으로 새지 않는다.</summary>
    private sealed class NotFoundSignal : Exception;
}
