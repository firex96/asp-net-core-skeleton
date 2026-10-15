using System.Reflection;
using System.Runtime.CompilerServices;
using AspNetCoreSkeletonApplication.Common.Errors;

namespace AspNetCoreSkeletonApplication.UnitTests.Conventions;

/// <summary>
/// 규약 테스트가 검사하는 앱 어셈블리와 계층 판정. 계층은 이름이 아니라 <b>네임스페이스</b>로 정한다 —
/// 이름 접미사와 네임스페이스가 맞는지는 <see cref="NamingConventionTests"/> 가 따로 본다.
/// </summary>
internal static partial class ConventionScan
{
    public static IReadOnlyList<Assembly> Assemblies { get; } = CreateAssemblies();

    /// <summary>스캔 대상의 모든 타입. 컴파일러가 만든 타입(람다 클로저 등)은 뺀다.</summary>
    public static IReadOnlyList<Type> Types { get; } = Assemblies
        .SelectMany(assembly => assembly.GetTypes())
        .Where(type => !type.Name.Contains('<') && !type.IsDefined(typeof(CompilerGeneratedAttribute), false))
        .ToArray();

    public static IEnumerable<Type> TypesIn(Layer layer) => Types.Where(type => LayerOf(type) == layer);

    public static Layer? LayerOf(Type type)
    {
        var ns = type.Namespace ?? string.Empty;
        return ns switch
        {
            _ when ns.EndsWith(".Controllers", StringComparison.Ordinal) => Layer.Controller,
            _ when ns.EndsWith(".Jobs", StringComparison.Ordinal) => Layer.Job,
            _ when ns.EndsWith(".Services.Mapping", StringComparison.Ordinal) => Layer.MappingService,
            _ when ns.EndsWith(".Services.Biz", StringComparison.Ordinal) => Layer.BizService,
            _ when ns.EndsWith(".Persistence.Services", StringComparison.Ordinal) => Layer.EntityService,
            _ when ns.EndsWith(".Domain", StringComparison.Ordinal) => Layer.Domain,
            _ when ns.EndsWith(".Mappers", StringComparison.Ordinal) => Layer.Mapper,
            _ when ns.EndsWith(".Clients", StringComparison.Ordinal) => Layer.Client,
            _ when ns.EndsWith(".Models", StringComparison.Ordinal) => Layer.Model,
            _ when ns.EndsWith(".Dtos", StringComparison.Ordinal) => Layer.Dto,
            _ when ns.Contains(".Persistence.Entities", StringComparison.Ordinal) => Layer.Entity,
            _ => null,
        };
    }

    /// <summary>스캔 대상 어셈블리에 정의된 타입인가 — 프레임워크 타입과 구분한다.</summary>
    public static bool IsAppType(Type type) => Assemblies.Contains(type.Assembly);

    /// <summary>제네릭 인자까지 펼친 타입들 (<c>Task&lt;IReadOnlyList&lt;X&gt;&gt;</c> → Task, IReadOnlyList, X).</summary>
    public static IEnumerable<Type> Unwrap(Type type)
    {
        var element = type.HasElementType ? type.GetElementType()! : type;
        yield return element;
        if (element.IsGenericType)
        {
            foreach (var argument in element.GetGenericArguments().SelectMany(Unwrap))
            {
                yield return argument;
            }
        }
    }

    /// <summary>위반 목록이 비어 있지 않으면 한 줄씩 보여 주며 실패한다.</summary>
    public static void AssertNoViolations(IEnumerable<string> violations)
    {
        var list = violations.ToArray();
        Assert.True(list.Length == 0, $"규약 위반 {list.Length}건:{Environment.NewLine}{string.Join(Environment.NewLine, list)}");
    }

    private static List<Assembly> CreateAssemblies()
    {
        List<Assembly> assemblies = [typeof(ErrorCode).Assembly, typeof(Program).Assembly];
        AddInternalApiAssembly(assemblies);
        AddBatchAssembly(assemblies);
        return assemblies;
    }

    // 템플릿에서 빠질 수 있는 앱은 그 앱 폴더(InternalApi/ · Batch/)에서 구현한다 — 앱이 빠지면 구현도 같이 빠지고 호출은 사라진다.
    static partial void AddInternalApiAssembly(List<Assembly> assemblies);

    static partial void AddBatchAssembly(List<Assembly> assemblies);
}
