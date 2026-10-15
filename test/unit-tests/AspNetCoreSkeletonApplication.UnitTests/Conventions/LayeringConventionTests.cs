using System.Reflection;
using AspNetCoreSkeletonApplication.Common.Persistence;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AspNetCoreSkeletonApplication.UnitTests.Conventions;

/// <summary>
/// 계층별 주입 허용 목록 — README 「요청 흐름」 의 표가 정본이다. 허용 목록 방식이라 예외 케이스가 없다:
/// 목록에 없는 타입을 주입하면 실패한다. (Entity · DbContext 를 앱이 못 보는 것은 internal 로 컴파일러가 막는다.)
/// </summary>
public class LayeringConventionTests
{
    public static TheoryData<Layer> LayersWithInjectionRules => [Layer.Controller, Layer.MappingService, Layer.BizService, Layer.EntityService];

    [Fact]
    public void Scan_FindsTypesInEveryAppAssembly()
    {
        Assert.All(ConventionScan.Assemblies, assembly =>
            Assert.Contains(ConventionScan.Types, type => type.Assembly == assembly));
    }

    [Theory]
    [MemberData(nameof(LayersWithInjectionRules))]
    public void ConstructorInjection_StaysInAllowList(Layer layer)
    {
        var violations =
            from type in ConventionScan.TypesIn(layer)
            where type is { IsClass: true, IsAbstract: false }
            from constructor in type.GetConstructors()
            from parameter in constructor.GetParameters()
            where !IsAllowed(layer, parameter.ParameterType)
            select $"{type.FullName} ← {parameter.ParameterType.Name} {parameter.Name}";

        ConventionScan.AssertNoViolations(violations);
    }

    [Fact]
    public void ControllerActions_UseOnlyModelsFromTheApp()
    {
        var violations =
            from controller in ConventionScan.TypesIn(Layer.Controller)
            from method in controller.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            from signatureType in method.GetParameters().Select(parameter => parameter.ParameterType).Append(method.ReturnType)
            from type in ConventionScan.Unwrap(signatureType)
            where ConventionScan.IsAppType(type) && ConventionScan.LayerOf(type) != Layer.Model
            select $"{controller.Name}.{method.Name} 의 시그니처에 {type.FullName} — 요청과 응답은 Model 만";

        ConventionScan.AssertNoViolations(violations);
    }

    private static bool IsAllowed(Layer layer, Type dependency)
    {
        if (IsGenericOf(dependency, typeof(ILogger<>)))
        {
            return true;
        }

        var name = dependency.Name;
        return layer switch
        {
            Layer.Controller => name.EndsWith("MappingService", StringComparison.Ordinal),
            Layer.MappingService => name.EndsWith("BizService", StringComparison.Ordinal)
                                    || IsGenericOf(dependency, typeof(IOptions<>))
                                    || dependency == typeof(TimeProvider),
            Layer.BizService => (dependency.IsInterface && name.StartsWith('I') && name.EndsWith("EntityService", StringComparison.Ordinal))
                                || dependency == typeof(IMainTransaction)
                                || ConventionScan.LayerOf(dependency) == Layer.Client
                                || (name.EndsWith("BizService", StringComparison.Ordinal) && ConventionScan.LayerOf(dependency) == Layer.BizService)
                                || IsGenericOf(dependency, typeof(IOptions<>))
                                || dependency == typeof(TimeProvider),
            Layer.EntityService => name == "AppDbContext",
            _ => false,
        };
    }

    private static bool IsGenericOf(Type type, Type openGeneric) =>
        type.IsGenericType && type.GetGenericTypeDefinition() == openGeneric;
}
