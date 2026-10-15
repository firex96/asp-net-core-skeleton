using System.Text.Json.Serialization;
using AspNetCoreSkeletonApplication.Common.Persistence.Caching;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.StackExchangeRedis;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AspNetCoreSkeletonApplication.Common.Web;

/// <summary>
/// 웹 앱이 공통으로 쓰는 HTTP 파이프라인 구성. 앱의 Program.cs 가 <see cref="AddSkeletonWeb"/> 와
/// <see cref="UseSkeletonWeb"/>(맨 앞) · <see cref="UseAuditorContext"/>(인증 뒤)를 부른다.
/// </summary>
public static class WebServiceCollectionExtensions
{
    public static IServiceCollection AddSkeletonWeb(this IServiceCollection services)
    {
        // 모든 에러 응답은 ProblemDetails 다. 어디서 만들어졌든 code · divisionCode 를 붙이고,
        // traceId 는 기본값(Activity.Id) 대신 응답 헤더 X-Trace-Id 와 같은 값으로 맞춘다.
        services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
        {
            context.ProblemDetails.AddErrorCodeIfMissing();
            context.ProblemDetails.Extensions[ProblemDetailsExtensions.TraceIdKey] = context.HttpContext.TraceIdentifier;
        });
        services.AddExceptionHandler<GlobalExceptionHandler>();

        services.AddOptions<AuditOptions>().BindConfiguration("Audit");

        // Data Protection 키(쿠키 · antiforgery 토큰을 암호화한다)를 Redis 에 둔다 — 인스턴스가 여러 개여도 서로의 쿠키를 읽는다.
        services.AddSingleton<DataProtectionRedis>();
        services.AddDataProtection().SetApplicationName("asp-net-core-skeleton-application");
        services.AddOptions<KeyManagementOptions>().Configure<DataProtectionRedis, IOptions<CacheOptions>>((keys, redis, cache) =>
            keys.XmlRepository = new RedisXmlRepository(() => redis.Database, $"{cache.Value.KeyPrefix}data-protection-keys"));

        // 열거형은 숫자가 아니라 외부 코드([JsonStringEnumMemberName])로 주고받는다 — JSON 본문도, 쿼리 · 라우트도.
        services.Configure<JsonOptions>(options =>
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        services.Configure<MvcOptions>(options =>
            options.ModelBinderProviders.Insert(0, new StringEnumModelBinderProvider()));

        return services;
    }

    /// <summary>
    /// 파이프라인 맨 앞에 둔다. 추적 ID 를 먼저 정하고(에러 응답 · 느린 요청 로그에도 실리도록), 요청 시간을 잰 뒤,
    /// 그 안에서 나는 예외를 모두 ProblemDetails 로 바꾼다.
    /// </summary>
    public static IApplicationBuilder UseSkeletonWeb(this IApplicationBuilder app)
    {
        app.UseMiddleware<TraceIdMiddleware>();
        app.UseMiddleware<SlowRequestMiddleware>();
        app.UseExceptionHandler();
        // 본문 없는 4xx · 5xx(인증 실패 401, 인가 실패 403, 없는 경로 404 등)도 ProblemDetails 로 채운다.
        app.UseStatusCodePages();

        return app;
    }

    /// <summary>
    /// 요청의 감사 주체를 정한다. 인증 주체를 읽으므로 <c>UseAuthentication</c> · <c>UseAuthorization</c> 뒤에 둔다.
    /// </summary>
    public static IApplicationBuilder UseAuditorContext(this IApplicationBuilder app)
    {
        app.UseMiddleware<AuditorContextMiddleware>();

        return app;
    }
}
