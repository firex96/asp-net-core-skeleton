using AspNetCoreSkeletonApplication.Common.Persistence.Entities.Main;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AspNetCoreSkeletonApplication.Common.Persistence.Configurations;

internal sealed class ProductEntityConfiguration : IEntityTypeConfiguration<ProductEntity>
{
    public void Configure(EntityTypeBuilder<ProductEntity> builder)
    {
        builder.ToTable("product");
        builder.Property(product => product.Name).HasMaxLength(200);
        builder.Property(product => product.Price).HasPrecision(18, 2);
        // 열거형은 이름 문자열로 저장한다 — 순서를 바꿔도 기존 데이터의 뜻이 바뀌지 않는다.
        builder.Property(product => product.Status).HasConversion<string>().HasMaxLength(20);
    }
}
