using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace AspNetCoreSkeletonApplication.Common.Configuration;

public static class LayeredServiceCollectionExtensions
{
    private const string MappingServiceSuffix = "MappingService";
    private const string BizServiceSuffix = "BizService";
    private const string EntityServiceSuffix = "EntityService";

    /// <summary>
    /// 어셈블리의 계층 서비스를 이름 접미사로 찾아 scoped 로 등록한다 — 서비스를 추가할 때 등록을 따로 하지 않는다.
    /// <list type="bullet">
    /// <item><c>~MappingService</c> · <c>~BizService</c> — 자기 타입으로</item>
    /// <item><c>~EntityService</c> — 같은 이름의 인터페이스 <c>I~EntityService</c> 로</item>
    /// </list>
    /// 접미사와 네임스페이스가 맞는지는 규약 테스트가 확인한다.
    /// </summary>
    public static IServiceCollection AddLayeredServices(this IServiceCollection services, Assembly assembly)
    {
        var concreteTypes = assembly.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false });

        foreach (var type in concreteTypes)
        {
            if (type.Name.EndsWith(MappingServiceSuffix, StringComparison.Ordinal)
                || type.Name.EndsWith(BizServiceSuffix, StringComparison.Ordinal))
            {
                services.AddScoped(type);
            }
            else if (type.Name.EndsWith(EntityServiceSuffix, StringComparison.Ordinal))
            {
                var serviceInterface = type.GetInterface($"I{type.Name}")
                    ?? throw new InvalidOperationException(
                        $"{type.FullName} 는 같은 이름의 인터페이스 I{type.Name} 를 구현해야 한다.");
                services.AddScoped(serviceInterface, type);
            }
        }

        return services;
    }
}
