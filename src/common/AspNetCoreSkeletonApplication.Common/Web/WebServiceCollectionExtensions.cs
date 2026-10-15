using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

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

        // 열거형은 숫자가 아니라 외부 코드([JsonStringEnumMemberName])로 주고받는다 — JSON 본문도, 쿼리 · 라우트도.
        services.Configure<JsonOptions>(options =>
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        services.Configure<MvcOptions>(options =>
            options.ModelBinderProviders.Insert(0, new StringEnumModelBinderProvider()));

        return services;
    }

    /// <summary>
    /// 파이프라인 맨 앞에 둔다. 추적 ID 를 먼저 정하고(에러 응답에도 실리도록), 그 뒤에서 나는 예외를 모두
    /// ProblemDetails 로 바꾼다.
    /// </summary>
    public static IApplicationBuilder UseSkeletonWeb(this IApplicationBuilder app)
    {
        app.UseMiddleware<TraceIdMiddleware>();
        app.UseExceptionHandler();

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
