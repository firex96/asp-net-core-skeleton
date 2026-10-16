using AspNetCoreSkeletonApplication.Common.Authorization.Services.Biz;
using AspNetCoreSkeletonApplication.Common.Diagnostics;
using AspNetCoreSkeletonApplication.Common.Persistence;
using AspNetCoreSkeletonApplication.Common.Persistence.Caching;
using AspNetCoreSkeletonApplication.Common.Persistence.Context;
using AspNetCoreSkeletonApplication.Common.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Backplane.StackExchangeRedis;
using ZiggyCreatures.Caching.Fusion.Serialization.SystemTextJson;

namespace AspNetCoreSkeletonApplication.Common.Configuration;

public static class CommonServiceCollectionExtensions
{
    /// <summary>
    /// Common 을 쓰는 앱이 부르는 등록 진입점. DB 컨텍스트, DB 헬스 체크, Common 의 계층 서비스
    /// (EntityService · 공유 BizService), 감사 필드 · 느린 쿼리 인터셉터, 캐시(FusionCache 를 HybridCache 로 — 메모리 + Redis + 백플레인), TimeProvider 를 등록한다.
    /// </summary>
    public static IServiceCollection AddCommon(this IServiceCollection services)
    {
        // 연결 문자열은 첫 DbContext 생성 시 읽는다 — 빠져 있으면 GetRequiredConnectionString 이 키 이름을 담아 멈춘다.
        services.AddOptions<DiagnosticsOptions>().BindConfiguration("Diagnostics");
        services.AddSingleton<AuditSaveChangesInterceptor>();
        services.AddSingleton<SlowQueryInterceptor>();
        services.AddDbContextPool<AppDbContext>((serviceProvider, options) => options
            .UseSqlServer(serviceProvider.GetRequiredService<IConfiguration>().GetRequiredConnectionString("Default"))
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
            .AddInterceptors(
                serviceProvider.GetRequiredService<AuditSaveChangesInterceptor>(),
                serviceProvider.GetRequiredService<SlowQueryInterceptor>()));
        services.AddScoped<IMainTransaction, MainTransaction>();

        // 캐시 — FusionCache: 메모리(L1) + Redis(L2) + Redis 백플레인. 코드는 HybridCache 추상화로만 쓴다(AsHybridCache).
        // 어느 인스턴스에서 무효화하든 백플레인이 모든 인스턴스의 L1 에서 같은 키 · 태그를 지운다 — 낡은 L1 이 남지 않는다.
        // 연결 문자열은 첫 사용 때 읽는다(빠지면 키 이름을 담아 멈춘다).
        services.AddOptions<CacheOptions>().BindConfiguration("Cache");
        services.AddStackExchangeRedisCache(_ => { });
        services.AddOptions<RedisCacheOptions>().Configure<IConfiguration>((redis, configuration) =>
            redis.Configuration = configuration.GetRequiredConnectionString("Redis"));
        services.AddOptions<RedisBackplaneOptions>().Configure<IConfiguration>((backplane, configuration) =>
            backplane.Configuration = configuration.GetRequiredConnectionString("Redis"));
        services.AddFusionCache()
            .WithOptions((serviceProvider, fusion) =>
            {
                var cache = serviceProvider.GetRequiredService<IOptions<CacheOptions>>().Value;
                fusion.CacheKeyPrefix = $"{cache.KeyPrefix}cache:";
                fusion.DefaultEntryOptions.Duration = cache.Expiration;
            })
            .WithSerializer(new FusionCacheSystemTextJsonSerializer())
            .WithRegisteredDistributedCache()
            .WithStackExchangeRedisBackplane(_ => { })
            .AsHybridCache();
        services.AddScoped<EntityCache>();

        // 현재 시각은 TimeProvider 로만 읽는다 — 테스트에서 FakeTimeProvider 로 바꿔 끼운다.
        services.TryAddSingleton(TimeProvider.System);

        services.AddHealthChecks()
            .AddDbContextCheck<AppDbContext>();

        services.AddLayeredServices(typeof(CommonServiceCollectionExtensions).Assembly);
        // 권한 판정의 입구 — 구현을 바꿀 때(예: ReBAC) 이 줄만 바꾼다.
        services.AddScoped<IPermissionEvaluator>(serviceProvider => serviceProvider.GetRequiredService<PermissionBizService>());

        return services;
    }
}
