using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace AspNetCoreSkeletonApplication.Common.Web;

/// <summary>
/// CSRF 방어 — 브라우저가 붙이는 <c>Sec-Fetch-Site</c> 가 <c>cross-site</c> 인 변경 요청(POST · PUT · PATCH · DELETE)을 막는다.
/// 쿠키의 SameSite=Strict 와 겹쳐 두는 두 번째 방어선이다. 헤더가 없는 요청(서버 간 호출, 오래된 브라우저)은 막지 않는다 —
/// 그런 요청은 브라우저 쿠키를 싣지 않거나 SameSite 가 막는다. SPA 코드는 바꿀 것이 없다.
/// <para>
/// 응답은 받는 쪽이 읽는 형식으로 준다 — API 는 403(본문은 <c>UseStatusCodePages</c> 의 ProblemDetails), OAuth 엔드포인트(<c>/connect/*</c>)는
/// OAuth 클라이언트가 읽는 오류 형식(RFC 6749 5.2: 400 <c>{"error":"invalid_request", "error_description": …}</c>)이다.
/// </para>
/// </summary>
internal sealed class FetchMetadataMiddleware(RequestDelegate next)
{
    private static readonly PathString OAuthEndpoints = new("/connect");

    public Task InvokeAsync(HttpContext context)
    {
        var request = context.Request;
        if (HttpMethods.IsGet(request.Method) || HttpMethods.IsHead(request.Method) || HttpMethods.IsOptions(request.Method)
            || !string.Equals(request.Headers["Sec-Fetch-Site"], "cross-site", StringComparison.OrdinalIgnoreCase))
        {
            return next(context);
        }

        if (request.Path.StartsWithSegments(OAuthEndpoints))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.Headers.CacheControl = "no-store";
            return context.Response.WriteAsJsonAsync(
                new Dictionary<string, string>
                {
                    ["error"] = "invalid_request",
                    ["error_description"] = "다른 사이트에서 보낸 요청이라 처리하지 않았어요.",
                },
                JsonSerializerOptions.Default,
                context.RequestAborted);
        }

        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    }
}
