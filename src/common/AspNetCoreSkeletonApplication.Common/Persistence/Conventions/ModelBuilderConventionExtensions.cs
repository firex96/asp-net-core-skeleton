using System.Linq.Expressions;
using AspNetCoreSkeletonApplication.Common.Persistence.Entities.Backbone;
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

    /// <summary>
    /// <see cref="ISoftDeletable"/> 엔티티마다 <c>DeletedAt == null</c> 전역 쿼리 필터를 건다 — 지워진 행은 모든 조회에서 빠진다.
    /// 지워진 행까지 봐야 하면 그 쿼리에서만 <c>IgnoreQueryFilters()</c> 를 쓴다.
    /// </summary>
    public static ModelBuilder UseSoftDeleteFilter(this ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes().Where(type => typeof(ISoftDeletable).IsAssignableFrom(type.ClrType)))
        {
            var entity = Expression.Parameter(entityType.ClrType, "entity");
            var notDeleted = Expression.Equal(
                Expression.Property(entity, nameof(ISoftDeletable.DeletedAt)),
                Expression.Constant(null, typeof(DateTimeOffset?)));
            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(Expression.Lambda(notDeleted, entity));
        }

        return modelBuilder;
    }
}
