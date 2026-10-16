using AspNetCoreSkeletonApplication.Common.Authorization.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AspNetCoreSkeletonApplication.Common.Authorization.Persistence.Configurations;

internal sealed class RoleEntityConfiguration : IEntityTypeConfiguration<RoleEntity>
{
    public void Configure(EntityTypeBuilder<RoleEntity> builder)
    {
        builder.ToTable("role");
        builder.Property(role => role.Name).HasMaxLength(100);
        builder.Property(role => role.Description).HasMaxLength(500);
        builder.HasIndex(role => role.Name).IsUnique().HasFilter("[DeletedAt] IS NULL");
    }
}
