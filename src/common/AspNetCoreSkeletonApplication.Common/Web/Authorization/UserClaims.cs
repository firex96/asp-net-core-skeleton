using System.Globalization;
using System.Security.Claims;

namespace AspNetCoreSkeletonApplication.Common.Web.Authorization;

public static class UserClaims
{
    /// <summary>
    /// 로그인한 사용자의 ID — 쿠키는 <c>NameIdentifier</c>, 토큰은 <c>sub</c>. 익명이거나 숫자가 아니면(클라이언트 토큰의 <c>client:{id}</c>) null.
    /// </summary>
    public static long? IdOf(ClaimsPrincipal user) =>
        long.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub"), NumberStyles.None, CultureInfo.InvariantCulture, out var id)
            ? id
            : null;
}
