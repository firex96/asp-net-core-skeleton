using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json.Serialization;

namespace AspNetCoreSkeletonApplication.Common.Enums;

/// <summary>
/// 열거형 멤버의 외부 코드. 코드는 멤버의 <see cref="JsonStringEnumMemberNameAttribute"/> 하나가 정본이고,
/// JSON · DB 컬럼 · 쿼리 바인딩이 모두 이 값을 쓴다 — C# 멤버 이름을 바꿔도 저장된 데이터와 API 계약이 바뀌지 않는다.
/// 모든 멤버에 코드가 있는지는 규약 테스트가 확인한다.
/// </summary>
public static class EnumCodes
{
    private static readonly ConcurrentDictionary<Type, Map> Maps = new();

    public static string ToCode<TEnum>(TEnum value) where TEnum : struct, Enum =>
        MapOf(typeof(TEnum)).ByValue[value];

    public static TEnum Parse<TEnum>(string code) where TEnum : struct, Enum =>
        TryParse(typeof(TEnum), code, out var value)
            ? (TEnum)value
            : throw new InvalidOperationException($"{typeof(TEnum).Name} 에 코드 '{code}' 가 없다");

    public static bool TryParse(Type enumType, string? code, out Enum value)
    {
        value = null!;
        if (code is null || !MapOf(enumType).ByCode.TryGetValue(code, out var found))
        {
            return false;
        }

        value = found;
        return true;
    }

    private static Map MapOf(Type enumType) => Maps.GetOrAdd(enumType, static type =>
    {
        var pairs = type.GetFields(BindingFlags.Public | BindingFlags.Static)
            .Select(field => (
                Value: (Enum)field.GetValue(null)!,
                Code: field.GetCustomAttribute<JsonStringEnumMemberNameAttribute>()?.Name
                      ?? throw new InvalidOperationException(
                          $"{type.Name}.{field.Name} 에 [JsonStringEnumMemberName] 외부 코드가 없다")))
            .ToArray();
        return new Map(
            pairs.ToDictionary(pair => pair.Value, pair => pair.Code),
            pairs.ToDictionary(pair => pair.Code, pair => pair.Value, StringComparer.Ordinal));
    });

    private sealed record Map(IReadOnlyDictionary<Enum, string> ByValue, IReadOnlyDictionary<string, Enum> ByCode);
}
