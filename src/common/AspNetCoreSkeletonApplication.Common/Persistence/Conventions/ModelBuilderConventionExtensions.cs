using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace AspNetCoreSkeletonApplication.Common.Persistence.Conventions;

internal static class ModelBuilderConventionExtensions
{
    /// <summary>
    /// 모든 엔티티의 열거형 속성(nullable 포함)을 외부 코드 문자열로 저장한다. 엔티티 구성에서 따로 지정하지 않는다 —
    /// 구성마다 적으면 빠뜨리는 곳이 생긴다. 컬럼 길이는 엔티티 구성의 <c>HasMaxLength</c> 로 정한다.
    /// </summary>
    public static ModelBuilder UseStringEnumCodes(this ModelBuilder modelBuilder)
    {
        foreach (var property in modelBuilder.Model.GetEntityTypes().SelectMany(entityType => entityType.GetProperties()))
        {
            var enumType = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;
            if (enumType.IsEnum)
            {
                var converterType = typeof(StringEnumValueConverter<>).MakeGenericType(enumType);
                property.SetValueConverter((ValueConverter)Activator.CreateInstance(converterType)!);
            }
        }

        return modelBuilder;
    }
}
