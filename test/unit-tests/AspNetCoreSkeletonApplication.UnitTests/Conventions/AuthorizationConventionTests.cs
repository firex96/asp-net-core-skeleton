using System.Reflection;
using Microsoft.AspNetCore.Authorization;

namespace AspNetCoreSkeletonApplication.UnitTests.Conventions;

/// <summary>
/// 컨트롤러는 역할 이름이 아니라 정책으로 인가한다 — <c>[Authorize(Roles = "…")]</c> 금지, <c>[Authorize(Policy = Policies.…)]</c> 를 쓴다.
/// 역할 문자열이 컨트롤러마다 흩어지면 역할 구성을 바꿀 때 빠뜨린다. 정책은 한 곳(AddSkeletonIdentity)에서 역할로 풀린다.
/// </summary>
public class AuthorizationConventionTests
{
    [Fact]
    public void Controllers_AuthorizeByPolicyNotRoles()
    {
        var violations =
            from controller in ConventionScan.TypesIn(Layer.Controller)
            from member in controller.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Cast<MemberInfo>().Prepend(controller)
            from authorize in member.GetCustomAttributes<AuthorizeAttribute>()
            where authorize.Roles is not null
            select $"{controller.Name}.{member.Name} — Roles 대신 Policy 로 인가한다";

        ConventionScan.AssertNoViolations(violations);
    }
}
