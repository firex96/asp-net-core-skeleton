using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace AspNetCoreSkeletonApplication.FunctionalTests.Support;

/// <summary>
/// 최소 API 의 <c>TypedResults.Problem</c> 이 쓰는 경로(<c>ProblemHttpResult</c> → <c>IProblemDetailsService</c>)를 확인하려고 앞에 붙이는 분기.
/// <c>/error-probe/minimal-problem/{상태}</c> 로 부르면 그 상태와 내부 문장을 담은 ProblemDetails 를 돌려준다.
/// </summary>
public sealed class MinimalProblemProbeStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
    {
        // Map 은 접두사 분기다 — 상태는 남은 경로(/500)에서 읽는다
        app.Map("/error-probe/minimal-problem", branch => branch.Run(context =>
            TypedResults.Problem(detail: ErrorProbeController.InternalDetail, statusCode: int.Parse(context.Request.Path.Value!.TrimStart('/')))
                .ExecuteAsync(context)));
        next(app);
    };
}
