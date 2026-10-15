using AspNetCoreSkeletonApplication.Common.Diagnostics;
using AspNetCoreSkeletonApplication.Common.Persistence;
using AspNetCoreSkeletonApplication.Common.Persistence.Context;
using AspNetCoreSkeletonApplication.Common.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AspNetCoreSkeletonApplication.Common.Configuration;

public static class CommonServiceCollectionExtensions
{
    /// <summary>
    /// Common 을 쓰는 앱이 부르는 등록 진입점. DB 컨텍스트, DB 헬스 체크, Common 의 계층 서비스
    /// (EntityService · 공유 BizService), 감사 필드 · 느린 쿼리 인터셉터, TimeProvider 를 등록한다.
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

        // 현재 시각은 TimeProvider 로만 읽는다 — 테스트에서 FakeTimeProvider 로 바꿔 끼운다.
        services.TryAddSingleton(TimeProvider.System);

        services.AddHealthChecks()
            .AddDbContextCheck<AppDbContext>();

        services.AddLayeredServices(typeof(CommonServiceCollectionExtensions).Assembly);

        return services;
    }
}
