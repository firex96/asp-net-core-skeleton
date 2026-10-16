using AspNetCoreSkeletonApplication.Common.Authorization.Dtos;

namespace AspNetCoreSkeletonApplication.Common.Authorization;

/// <summary>
/// 권한 코드 카탈로그 — 권한의 정본은 이 코드다. 역할이 어떤 권한을 갖는지는 DB(<c>role_permission</c>)에 두지만,
/// 코드 자체는 여기서만 만든다. 컨트롤러는 <c>[HasPermission(Permissions.Products.Manage)]</c> 로 선언한다(규약 테스트가 카탈로그 밖 코드를 막는다).
/// 코드는 <c>{대상}.{동작}</c> 소문자다.
/// </summary>
public static class Permissions
{
    public static class Products
    {
        /// <summary>상품 상태 변경 · 남의 상품 삭제.</summary>
        public const string Manage = "products.manage";
    }

    public static class Authorization
    {
        /// <summary>그룹 · 역할 · 권한 관리.</summary>
        public const string Manage = "authorization.manage";
    }

    public static readonly IReadOnlyList<PermissionDto> All =
    [
        new(Products.Manage, "상품 상태 변경과 다른 사용자의 상품 삭제"),
        new(Authorization.Manage, "사용자 그룹 · 역할 · 권한 관리"),
    ];

    public static bool Exists(string code) => All.Any(permission => permission.Code == code);
}
