using System.Globalization;
using System.Security.Claims;

namespace AspNetCoreSkeletonApplication.Common.Web.Authorization;

public static class UserClaims
{
    /// <summary>로그인한 사용자의 ID(<c>NameIdentifier</c>). 익명이거나 숫자가 아니면 null.</summary>
    public static long? IdOf(ClaimsPrincipal user) =>
        long.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), NumberStyles.None, CultureInfo.InvariantCulture, out var id) ? id : null;
}
