namespace AspNetCoreSkeletonApplication.UnitTests.Conventions;

/// <summary>
/// 이름 접미사는 계층을 드러낸다 — 접미사와 네임스페이스가 서로 맞아야 한다. 이 검사가 계층 판정(네임스페이스)의
/// 어긋남도 잡으므로, 다른 규약 테스트가 대상을 못 찾아 그냥 통과하는 일을 막는다.
/// </summary>
public class NamingConventionTests
{
    private static readonly (string Suffix, Layer Layer)[] SuffixLayers =
    [
        ("Controller", Layer.Controller),
        ("MappingService", Layer.MappingService),
        ("BizService", Layer.BizService),
        ("EntityService", Layer.EntityService),
        ("Client", Layer.Client),
        ("Mapper", Layer.Mapper),
        ("Model", Layer.Model),
        ("Dto", Layer.Dto),
        ("Entity", Layer.Entity),
    ];

    [Fact]
    public void LayerSuffix_LivesInItsLayerNamespace_AndBackAgain()
    {
        var violations = new List<string>();
        foreach (var type in ConventionScan.Types.Where(type => type.IsClass && !type.IsNested))
        {
            var bySuffix = SuffixLayers.Where(pair => type.Name.EndsWith(pair.Suffix, StringComparison.Ordinal))
                .Select(pair => (Layer?)pair.Layer).FirstOrDefault();
            var byNamespace = ConventionScan.LayerOf(type);

            if (bySuffix is not null && bySuffix != byNamespace)
            {
                violations.Add($"{type.FullName} — 이름은 {bySuffix} 인데 네임스페이스는 {byNamespace?.ToString() ?? "계층 밖"}");
            }

            if (byNamespace is { } layer && ExpectsSuffix(layer) && bySuffix != layer)
            {
                violations.Add($"{type.FullName} — {layer} 네임스페이스의 클래스는 그 접미사로 끝난다");
            }
        }

        ConventionScan.AssertNoViolations(violations);
    }

    [Fact]
    public void BareServiceSuffix_IsNotUsed()
    {
        var violations = ConventionScan.Types
            .Where(type => type.IsClass && type.Name.EndsWith("Service", StringComparison.Ordinal))
            .Where(type => !type.Name.EndsWith("MappingService", StringComparison.Ordinal)
                           && !type.Name.EndsWith("BizService", StringComparison.Ordinal)
                           && !type.Name.EndsWith("EntityService", StringComparison.Ordinal))
            .Select(type => $"{type.FullName} — 맨 Service 는 계층을 숨긴다");

        ConventionScan.AssertNoViolations(violations);
    }

    [Fact]
    public void EntityService_ImplementsSameNamedInterface()
    {
        var violations = ConventionScan.TypesIn(Layer.EntityService)
            .Where(type => type is { IsClass: true, IsAbstract: false })
            .Where(type => type.GetInterface($"I{type.Name}") is null)
            .Select(type => $"{type.FullName} — I{type.Name} 를 구현해야 한다");

        ConventionScan.AssertNoViolations(violations);
    }

    // Entity 네임스페이스에는 Backbone 기반 클래스가, Mapper 네임스페이스에는 MapperDefaults 같은 비 클래스가 있어
    // 역방향(네임스페이스 → 접미사)은 계층 서비스 · 컨트롤러 · 클라이언트에만 요구한다.
    private static bool ExpectsSuffix(Layer layer) =>
        layer is Layer.Controller or Layer.MappingService or Layer.BizService or Layer.EntityService;
}
