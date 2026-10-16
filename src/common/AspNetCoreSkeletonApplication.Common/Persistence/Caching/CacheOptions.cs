namespace AspNetCoreSkeletonApplication.Common.Persistence.Caching;

/// <summary>구성 경로 <c>Cache</c>. 연결 문자열은 비밀값 <c>ConnectionStrings:Redis</c> 다.</summary>
public sealed class CacheOptions
{
    /// <summary>
    /// 메모리(L1) · Redis(L2) 공통 만료. L1 을 따로 짧게 두지 않는다 — 다른 인스턴스의 L1 은 백플레인이 무효화한다.
    /// </summary>
    public TimeSpan Expiration { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Redis 키 접두사. 같은 Redis 를 여러 서비스(또는 테스트)가 함께 쓸 때 키가 섞이지 않게 한다.
    /// 캐시 항목은 <c>{KeyPrefix}cache:</c> 아래, Data Protection 키는 <c>{KeyPrefix}data-protection-keys</c> 다.
    /// </summary>
    public string KeyPrefix { get; set; } = "asp-net-core-skeleton-application:";
}
