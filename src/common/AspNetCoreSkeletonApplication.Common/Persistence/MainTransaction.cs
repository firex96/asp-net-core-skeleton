using AspNetCoreSkeletonApplication.Common.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace AspNetCoreSkeletonApplication.Common.Persistence;

internal sealed class MainTransaction(AppDbContext dbContext) : IMainTransaction
{
    public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken)
    {
        if (dbContext.Database.CurrentTransaction is not null)
        {
            return await action(cancellationToken);
        }

        // 재시도 실행 전략을 켜도 깨지지 않도록 트랜잭션 전체를 전략 안에서 연다.
        var strategy = dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async token =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(token);
            var result = await action(token);
            await transaction.CommitAsync(token);
            return result;
        }, cancellationToken);
    }
}
