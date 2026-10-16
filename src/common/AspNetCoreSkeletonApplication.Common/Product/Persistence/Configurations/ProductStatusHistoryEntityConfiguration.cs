using AspNetCoreSkeletonApplication.Common.Product.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AspNetCoreSkeletonApplication.Common.Product.Persistence.Configurations;

internal sealed class ProductStatusHistoryEntityConfiguration : IEntityTypeConfiguration<ProductStatusHistoryEntity>
{
    public void Configure(EntityTypeBuilder<ProductStatusHistoryEntity> builder)
    {
        builder.ToTable("product_status_history");
        builder.Property(history => history.FromStatus).HasMaxLength(20);
        builder.Property(history => history.ToStatus).HasMaxLength(20);
        // 논리 FK — FOREIGN KEY 제약 대신 조회용 인덱스만 둔다.
        builder.HasIndex(history => history.ProductId);
    }
}
