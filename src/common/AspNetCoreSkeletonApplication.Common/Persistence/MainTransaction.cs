using AspNetCoreSkeletonApplication.Common.Persistence.Caching;
using AspNetCoreSkeletonApplication.Common.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace AspNetCoreSkeletonApplication.Common.Persistence;

internal sealed class MainTransaction(AppDbContext dbContext, EntityCache entityCache) : IMainTransaction
{
    public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken)
    {
        if (dbContext.Database.CurrentTransaction is not null)
        {
            return await action(cancellationToken);
        }

        // 재시도 실행 전략을 켜도 깨지지 않도록 트랜잭션 전체를 전략 안에서 연다.
        var strategy = dbContext.Database.CreateExecutionStrategy();
        var result = await strategy.ExecuteAsync(async token =>
        {
            entityCache.DiscardPending();
            try
            {
                await using var transaction = await dbContext.Database.BeginTransactionAsync(token);
                var value = await action(token);
                await transaction.CommitAsync(token);
                return value;
            }
            catch
            {
                entityCache.DiscardPending();
                throw;
            }
        }, cancellationToken);

        // 커밋이 끝난 뒤에 무효화한다 — 그 전이면 다른 요청이 옛 값을 다시 캐시할 수 있다.
        await entityCache.FlushPendingAsync(cancellationToken);
        return result;
    }
}
