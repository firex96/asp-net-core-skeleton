using AspNetCoreSkeletonApplication.Common.Persistence;

namespace AspNetCoreSkeletonApplication.FunctionalTests.Support;

/// <summary>DB 가 없으므로 트랜잭션 없이 바로 실행한다.</summary>
public sealed class InlineMainTransaction : IMainTransaction
{
    public Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken) =>
        action(cancellationToken);
}
