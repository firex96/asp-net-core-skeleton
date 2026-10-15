namespace AspNetCoreSkeletonApplication.Common.Persistence.Caching;

/// <summary>구성 경로 <c>Cache</c>. 연결 문자열은 비밀값 <c>ConnectionStrings:Redis</c> 다.</summary>
public sealed class CacheOptions
{
    /// <summary>Redis(L2)까지 포함한 전체 만료.</summary>
    public TimeSpan Expiration { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>
    /// 인스턴스 메모리(L1) 만료. 다른 인스턴스의 L1 은 무효화가 닿지 않으므로, 여러 인스턴스로 돌 때 낡은 값이 남는 시간의 상한이다.
    /// </summary>
    public TimeSpan LocalExpiration { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>Redis 키 접두사. 같은 Redis 를 여러 서비스(또는 테스트)가 함께 쓸 때 키가 섞이지 않게 한다.</summary>
    public string KeyPrefix { get; set; } = "asp-net-core-skeleton-application:";
}
