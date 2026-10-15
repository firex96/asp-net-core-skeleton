using System.Security.Claims;
using AspNetCoreSkeletonApplication.Common.Constants;
using AspNetCoreSkeletonApplication.Common.Contexts;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace AspNetCoreSkeletonApplication.Common.Web;

/// <summary>
/// 요청의 감사 주체를 정해 <see cref="AuditorContext"/> 에 싣는다. 우선순위:
/// <list type="number">
/// <item>인증 주체 — <see cref="ClaimTypes.NameIdentifier"/> 또는 <c>sub</c> claim</item>
/// <item><c>X-User-Id</c> 헤더 — <see cref="AuditOptions.TrustUserHeader"/> 가 켜졌을 때만</item>
/// <item>없음 — 감사 필드에는 <see cref="AuditorIds.System"/> 이 기록된다</item>
/// </list>
/// 값은 <see cref="long.TryParse(string?, out long)"/> 로만 읽는다 — 빈 값 · 숫자 아님 · 범위 초과는 버린다(자르면 남의 ID 가 된다).
/// 인증 뒤에 둔다(<c>UseAuditorContext()</c>).
/// </summary>
internal sealed class AuditorContextMiddleware(RequestDelegate next, IOptions<AuditOptions> options)
{
    public async Task InvokeAsync(HttpContext context)
    {
        using (AuditorContext.Begin(Resolve(context)))
        {
            await next(context);
        }
    }

    private long? Resolve(HttpContext context)
    {
        var claim = context.User.FindFirst(ClaimTypes.NameIdentifier) ?? context.User.FindFirst("sub");
        if (claim is not null)
        {
            return long.TryParse(claim.Value, out var authenticated) ? authenticated : null;
        }

        if (options.Value.TrustUserHeader
            && long.TryParse(context.Request.Headers[CustomHeaderNames.UserId], out var trusted))
        {
            return trusted;
        }

        return null;
    }
}
