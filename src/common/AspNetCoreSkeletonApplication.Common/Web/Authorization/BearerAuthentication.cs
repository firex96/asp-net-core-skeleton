using Microsoft.AspNetCore.Http;

namespace AspNetCoreSkeletonApplication.Common.Web.Authorization;

/// <summary>요청이 Bearer 토큰을 실었나 — 인증 스킴 선택(정책 스킴)과 권한 판정이 같은 기준을 쓴다.</summary>
public static class BearerAuthentication
{
    public const string SessionOrBearerScheme = "SessionOrBearer";

    public static bool IsBearer(HttpRequest? request) =>
        request is not null && request.Headers.Authorization.ToString().StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase);
}
