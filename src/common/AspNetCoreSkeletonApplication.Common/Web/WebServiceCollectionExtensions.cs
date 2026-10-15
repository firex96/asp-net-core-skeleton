using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace AspNetCoreSkeletonApplication.Common.Web;

/// <summary>
/// 웹 앱이 공통으로 쓰는 HTTP 파이프라인 구성. 앱의 Program.cs 가 <see cref="AddSkeletonWeb"/> 와
/// <see cref="UseSkeletonWeb"/> 를 짝으로 부른다.
/// </summary>
public static class WebServiceCollectionExtensions
{
    public static IServiceCollection AddSkeletonWeb(this IServiceCollection services)
    {
        // 모든 에러 응답은 ProblemDetails 다. 어디서 만들어졌든 code · divisionCode 확장 필드를 붙인다.
        services.AddProblemDetails(options =>
            options.CustomizeProblemDetails = context => context.ProblemDetails.AddErrorCodeIfMissing());
        services.AddExceptionHandler<GlobalExceptionHandler>();

        return services;
    }

    /// <summary>파이프라인 맨 앞에 둔다 — 뒤에서 나는 예외를 모두 ProblemDetails 로 바꾼다.</summary>
    public static IApplicationBuilder UseSkeletonWeb(this IApplicationBuilder app)
    {
        app.UseExceptionHandler();

        return app;
    }
}
