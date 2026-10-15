using System.Diagnostics;
using AspNetCoreSkeletonApplication.Common.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AspNetCoreSkeletonApplication.Common.Web;

/// <summary>
/// <see cref="DiagnosticsOptions.SlowRequestThreshold"/> 를 넘긴 요청을 경고로 남긴다. 쿼리 문자열은 남기지 않는다
/// (개인정보가 실릴 수 있다). 추적 ID 를 정한 뒤, 예외 처리보다 바깥에 둔다 — 실패한 요청의 시간도 잰다.
/// </summary>
internal sealed partial class SlowRequestMiddleware(
    RequestDelegate next,
    IOptionsMonitor<DiagnosticsOptions> options,
    ILogger<SlowRequestMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var started = Stopwatch.GetTimestamp();
        try
        {
            await next(context);
        }
        finally
        {
            var elapsed = Stopwatch.GetElapsedTime(started);
            if (elapsed >= options.CurrentValue.SlowRequestThreshold)
            {
                LogSlowRequest(logger, context.Request.Method, context.Request.Path, context.Response.StatusCode, elapsed.TotalMilliseconds);
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "느린 요청 {Method} {Path} → {StatusCode} {ElapsedMilliseconds:F0}ms")]
    private static partial void LogSlowRequest(ILogger logger, string method, string path, int statusCode, double elapsedMilliseconds);
}
