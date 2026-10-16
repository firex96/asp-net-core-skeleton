using System.Collections;
using System.Reflection;
using System.Text.Json.Serialization;
using AspNetCoreSkeletonApplication.Common.Persistence.Entities.Backbone;

namespace AspNetCoreSkeletonApplication.UnitTests.Conventions;

/// <summary>타입 선언 규칙 — Model · Dto 의 형태, 매퍼, 엔티티, Domain 의 순수성.</summary>
public class DeclarationConventionTests
{
    [Fact]
    public void ModelsAndDtos_AreSealedRecordsWithoutOptionalParameters()
    {
        // Mapperly 는 소스가 없는 선택적 생성자 매개변수를 조용히 기본값으로 채운다 — 그래서 기본값을 두지 않는다.
        var violations =
            from type in ConventionScan.Types
            where ConventionScan.LayerOf(type) is Layer.Model or Layer.Dto && !type.IsNested
            let problems = Problems(type).ToArray()
            where problems.Length > 0
            select $"{type.FullName} — {string.Join(", ", problems)}";

        ConventionScan.AssertNoViolations(violations);

        static IEnumerable<string> Problems(Type type)
        {
            if (!type.IsSealed)
            {
                yield return "sealed 가 아니다";
            }

            if (type.GetProperty("EqualityContract", BindingFlags.NonPublic | BindingFlags.Instance) is null)
            {
                yield return "record 가 아니다";
            }

            foreach (var parameter in type.GetConstructors().SelectMany(constructor => constructor.GetParameters()))
            {
                if (parameter.HasDefaultValue)
                {
                    yield return $"생성자 매개변수 {parameter.Name} 에 기본값";
                }
            }
        }
    }

    [Fact]
    public void Enums_DeclareUniqueExternalCodeOnEveryMember()
    {
        // 외부 코드가 JSON · DB · 쿼리의 계약이다 — C# 이름에 기대지 않는다.
        var violations = new List<string>();
        foreach (var enumType in ConventionScan.Types.Where(type => type.IsEnum))
        {
            var codes = enumType.GetFields(BindingFlags.Public | BindingFlags.Static)
                .Select(field => (field.Name, Code: field.GetCustomAttribute<JsonStringEnumMemberNameAttribute>()?.Name))
                .ToArray();
            violations.AddRange(codes.Where(member => member.Code is null)
                .Select(member => $"{enumType.FullName}.{member.Name} — [JsonStringEnumMemberName] 이 없다"));
            violations.AddRange(codes.Where(member => member.Code is not null)
                .GroupBy(member => member.Code).Where(group => group.Count() > 1)
                .Select(group => $"{enumType.FullName} — 코드 '{group.Key}' 가 중복이다"));
        }

        ConventionScan.AssertNoViolations(violations);
    }

    [Fact]
    public void Mappers_AreStatic()
    {
        var violations = ConventionScan.TypesIn(Layer.Mapper)
            .Where(type => type.IsClass && !(type.IsAbstract && type.IsSealed))
            .Select(type => $"{type.FullName} — 매퍼는 static partial 이다(DI 대상이 아니다)");

        ConventionScan.AssertNoViolations(violations);
    }

    [Fact]
    public void Entities_HaveNoNavigationProperties()
    {
        var violations =
            from type in ConventionScan.TypesIn(Layer.Entity)
            where type.IsClass && type.IsSubclassOf(typeof(BaseEntity))
            from property in type.GetProperties()
            where IsEntityReference(property.PropertyType)
            select $"{type.Name}.{property.Name} — navigation property 를 두지 않는다(외래 키는 long 컬럼으로)";

        ConventionScan.AssertNoViolations(violations);

        static bool IsEntityReference(Type type) =>
            typeof(BaseEntity).IsAssignableFrom(type)
            || (type != typeof(string) && typeof(IEnumerable).IsAssignableFrom(type)
                && ConventionScan.Unwrap(type).Any(argument => typeof(BaseEntity).IsAssignableFrom(argument)));
    }

    [Fact]
    public void Entities_DeriveFromBaseEntityWithLongId()
    {
        Assert.Equal(typeof(long), typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.PropertyType);

        var violations = ConventionScan.TypesIn(Layer.Entity)
            .Where(type => type is { IsClass: true, IsAbstract: false } && !type.IsSubclassOf(typeof(BaseEntity)))
            .Select(type => $"{type.FullName} — BaseEntity 를 상속한다");

        ConventionScan.AssertNoViolations(violations);
    }

    [Fact]
    public void Domain_ReferencesOnlyDtosEnumsErrorsAndSystem()
    {
        // 시그니처(생성자 · 메서드 · 필드 · 속성) 수준의 검사다 — EF · DI · ASP.NET 타입을 받거나 들고 있지 않는다.
        var violations =
            from type in ConventionScan.TypesIn(Layer.Domain)
            from referenced in ConventionScan.SignatureTypes(type).SelectMany(ConventionScan.Unwrap).Distinct()
            where !IsAllowedForDomain(referenced)
            select $"{type.FullName} → {referenced.FullName}";

        ConventionScan.AssertNoViolations(violations);
    }

    private static bool IsAllowedForDomain(Type type)
    {
        if (type.IsGenericParameter || (type.Namespace?.StartsWith("System", StringComparison.Ordinal) ?? false))
        {
            return !(type.Namespace?.StartsWith("System.Net.Http", StringComparison.Ordinal) ?? false);
        }

        return ConventionScan.IsAppType(type)
               && (ConventionScan.LayerOf(type) is Layer.Dto or Layer.Domain
                   || (type.Namespace?.EndsWith(".Enums", StringComparison.Ordinal) ?? false)
                   || (type.Namespace?.EndsWith(".Errors", StringComparison.Ordinal) ?? false));
    }
}
