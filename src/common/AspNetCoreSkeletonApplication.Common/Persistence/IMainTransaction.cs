namespace AspNetCoreSkeletonApplication.Common.Persistence;

/// <summary>
/// 주 DB 트랜잭션 경계. BizService 의 변경 메서드는 예외 없이 이 안에서 실행한다 — Spring 의 <c>@Transactional</c> 자리.
/// 같은 요청(scope)의 EntityService 들은 같은 DbContext 를 쓰므로 함께 커밋되고 함께 롤백된다.
/// 이미 트랜잭션 안이면 새로 열지 않고 참여한다.
/// </summary>
public interface IMainTransaction
{
    Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken);
}
