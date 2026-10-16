using Microsoft.AspNetCore.Authorization;

namespace AspNetCoreSkeletonApplication.Common.Web.Authorization;

/// <summary>
/// 권한 코드로 인가한다 — <c>[HasPermission(Permissions.Products.Manage)]</c>. 정책 이름 <c>Permission:{코드}</c> 를 만들고,
/// <see cref="PermissionPolicyProvider"/> 가 그 정책을 즉석에서 만든다(권한마다 정책을 등록하지 않는다). 코드는 카탈로그 상수만 쓴다(규약 테스트).
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class HasPermissionAttribute(string permission) : AuthorizeAttribute(PolicyNameOf(permission))
{
    public string Permission { get; } = permission;

    /// <summary>권한 코드의 정책 이름 — 코드에서 <c>IAuthorizationService.AuthorizeAsync(User, HasPermissionAttribute.PolicyNameOf(Permissions.…))</c> 로 판정할 때.</summary>
    public static string PolicyNameOf(string permission) => PermissionPolicyProvider.PolicyPrefix + permission;
}
