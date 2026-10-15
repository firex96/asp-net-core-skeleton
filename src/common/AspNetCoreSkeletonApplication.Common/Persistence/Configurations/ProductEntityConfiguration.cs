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
        // 열거형은 외부 코드 문자열로 저장된다(AppDbContext 의 UseStringEnumCodes) — 여기서는 길이만 정한다.
        builder.Property(product => product.Status).HasMaxLength(20);
    }
}
