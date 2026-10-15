using Microsoft.AspNetCore.Http;

namespace AspNetCoreSkeletonApplication.Common.Web;

/// <summary>
/// CSRF 방어 — 브라우저가 붙이는 <c>Sec-Fetch-Site</c> 가 <c>cross-site</c> 인 변경 요청(POST · PUT · PATCH · DELETE)을 403 으로 막는다.
/// 쿠키의 SameSite=Strict 와 겹쳐 두는 두 번째 방어선이다. 헤더가 없는 요청(서버 간 호출, 오래된 브라우저)은 막지 않는다 —
/// 그런 요청은 브라우저 쿠키를 싣지 않거나 SameSite 가 막는다. SPA 코드는 바꿀 것이 없다.
/// </summary>
internal sealed class FetchMetadataMiddleware(RequestDelegate next)
{
    public Task InvokeAsync(HttpContext context)
    {
        var request = context.Request;
        if (!HttpMethods.IsGet(request.Method) && !HttpMethods.IsHead(request.Method) && !HttpMethods.IsOptions(request.Method)
            && string.Equals(request.Headers["Sec-Fetch-Site"], "cross-site", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        }

        return next(context);
    }
}
