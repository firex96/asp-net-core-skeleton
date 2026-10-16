using System.Reflection;
using AspNetCoreSkeletonApplication.Common.Authorization;
using AspNetCoreSkeletonApplication.Common.Web.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace AspNetCoreSkeletonApplication.UnitTests.Conventions;

/// <summary>
/// 컨트롤러는 역할 이름으로 인가하지 않는다 — <c>[Authorize(Roles = "…")]</c> 금지. 권한은 <c>[HasPermission(Permissions.…)]</c> 로 선언하고,
/// 그 코드는 카탈로그(<see cref="Permissions"/>)에 있어야 한다. 사용자 → 그룹 → 역할 → 권한은 데이터라 코드에 역할 이름이 나오지 않는다.
/// </summary>
public class AuthorizationConventionTests
{
    [Fact]
    public void Controllers_DoNotAuthorizeByRoles()
    {
        var violations =
            from member in ControllerMembers()
            from authorize in member.Member.GetCustomAttributes<AuthorizeAttribute>()
            where authorize.Roles is not null
            select $"{member.Controller.Name}.{member.Member.Name} — Roles 대신 [HasPermission] 으로 인가한다";

        ConventionScan.AssertNoViolations(violations);
    }

    [Fact]
    public void HasPermission_UsesCatalogCodes()
    {
        var violations =
            from member in ControllerMembers()
            from attribute in member.Member.GetCustomAttributes<HasPermissionAttribute>()
            where !Permissions.Exists(attribute.Permission)
            select $"{member.Controller.Name}.{member.Member.Name} — '{attribute.Permission}' 는 Permissions 카탈로그에 없다";

        ConventionScan.AssertNoViolations(violations);
    }

    [Fact]
    public void Catalog_CodesAreUniqueLowercaseTargetDotAction()
    {
        var codes = Permissions.All.Select(permission => permission.Code).ToArray();

        Assert.Equal(codes.Length, codes.Distinct(StringComparer.Ordinal).Count());
        Assert.All(codes, code => Assert.Matches("^[a-z]+(-[a-z]+)*\\.[a-z]+(-[a-z]+)*$", code));
    }

    private static IEnumerable<(Type Controller, MemberInfo Member)> ControllerMembers() =>
        from controller in ConventionScan.TypesIn(Layer.Controller)
        from member in controller.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Cast<MemberInfo>().Prepend(controller)
        select (controller, member);
}
