using AspNetCoreSkeletonApplication.Common.Constants;
using AspNetCoreSkeletonApplication.Common.Contexts;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace AspNetCoreSkeletonApplication.Common.Web;

/// <summary>
/// 요청마다 추적 ID 를 정한다 — 받은 <c>X-Trace-Id</c> 가 유효하면 그 값, 아니면 새 값.
/// 그 값을 <see cref="TraceIdContext"/>, 로깅 scope(<c>XTraceId</c>), <see cref="HttpContext.TraceIdentifier"/>,
/// 응답 헤더에 싣는다. 파이프라인 맨 앞에 둔다.
/// </summary>
internal sealed class TraceIdMiddleware(RequestDelegate next, ILogger<TraceIdMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var traceId = TraceIdSanitizer.Sanitize(context.Request.Headers[CustomHeaderNames.TraceId]);

        context.TraceIdentifier = traceId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[CustomHeaderNames.TraceId] = traceId;
            return Task.CompletedTask;
        });

        // scope 키를 XTraceId 로 둔다 — ASP.NET Core 가 이미 넣는 W3C TraceId scope 와 이름이 겹치지 않게.
        using (TraceIdContext.Begin(traceId))
        using (logger.BeginScope("X-Trace-Id:{XTraceId}", traceId))
        {
            await next(context);
        }
    }
}
