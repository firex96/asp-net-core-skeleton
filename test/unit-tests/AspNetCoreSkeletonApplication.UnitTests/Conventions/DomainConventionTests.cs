namespace AspNetCoreSkeletonApplication.UnitTests.Conventions;

/// <summary>
/// 도메인 패키지 규칙 (예외 없음) — 업무 코드의 네임스페이스는 <c>{프로젝트}.{도메인}.{계층}</c> 이다.
/// 도메인 소속은 네임스페이스로만 드러내고, 도메인끼리는 BizService · Dto 로만 만난다.
/// </summary>
public class DomainConventionTests
{
    private static readonly HashSet<string> LayerSegments = new(StringComparer.Ordinal)
    {
        "Controllers", "Jobs", "Services", "Domain", "Mappers", "Clients", "Models", "Dtos",
    };

    [Fact]
    public void LayerNamespaces_LiveUnderADomain()
    {
        // 프로젝트 바로 아래에 계층이 오면(예: Common.Services.Biz) 어느 도메인 것인지 알 수 없다.
        var violations = ConventionScan.Types
            .Where(type => ConventionScan.LayerOf(type) is not null && ConventionScan.AreaOf(type) is { } area && LayerSegments.Contains(area))
            .Select(type => $"{type.FullName} — 계층 네임스페이스는 도메인 아래에 둔다({{프로젝트}}.{{도메인}}.{{계층}})");

        ConventionScan.AssertNoViolations(violations);
    }

    [Fact]
    public void PlatformAreas_HoldOnlyAbstractLayerTypes()
    {
        // 공통 기반 영역에는 BaseEntity · TracedJob 같은 기반 타입만 둔다 — 구체 업무 타입은 도메인에 속한다.
        var violations = ConventionScan.Types
            .Where(type => ConventionScan.LayerOf(type) is not null && ConventionScan.AreaOf(type) is { } area
                           && ConventionScan.PlatformAreas.Contains(area) && !type.IsAbstract)
            .Select(type => $"{type.FullName} — 공통 기반 영역의 계층 타입은 abstract 기반 타입만. 구체 타입은 도메인에 둔다");

        ConventionScan.AssertNoViolations(violations);
    }

    [Fact]
    public void Persistence_IsReferencedOnlyWithinItsDomain()
    {
        // 다른 도메인의 엔티티 · EntityService 를 직접 쓰면 그 도메인의 캐시 무효화 · 판단 규칙을 우회한다.
        var violations =
            from type in ConventionScan.Types
            let domain = ConventionScan.DomainOf(type)
            where domain is not null
            from referenced in ConventionScan.SignatureTypes(type).SelectMany(ConventionScan.Unwrap).Distinct()
            where ConventionScan.IsAppType(referenced) && IsPersistence(referenced)
            let referencedDomain = ConventionScan.DomainOf(referenced)
            where referencedDomain is not null && referencedDomain != domain
            select $"{type.FullName} → {referenced.FullName} — 다른 도메인({referencedDomain})의 영속 계층은 BizService · Dto 로만 쓴다";

        ConventionScan.AssertNoViolations(violations);

        static bool IsPersistence(Type type) =>
            type.Namespace?.Contains(".Persistence.", StringComparison.Ordinal) ?? false;
    }
}
