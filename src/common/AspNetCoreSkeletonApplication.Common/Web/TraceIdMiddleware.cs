using AspNetCoreSkeletonApplication.Common.Constants;
using AspNetCoreSkeletonApplication.Common.Contexts;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace AspNetCoreSkeletonApplication.Common.Web;

/// <summary>
/// 요청마다 추적 ID 를 정한다 — 받은 <c>X-Trace-Id</c> 가 유효하면 그 값, 아니면 새 값.
/// 그 값을 <see cref="TraceIdContext"/>, 로깅 scope(<c>TraceId</c>), <see cref="HttpContext.TraceIdentifier"/>,
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

        using (TraceIdContext.Begin(traceId))
        using (logger.BeginScope(new Dictionary<string, object> { ["TraceId"] = traceId }))
        {
            await next(context);
        }
    }
}
