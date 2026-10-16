using AspNetCoreSkeletonApplication.Common.Authorization.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AspNetCoreSkeletonApplication.Common.Authorization.Persistence.Configurations;

internal sealed class RolePermissionEntityConfiguration : IEntityTypeConfiguration<RolePermissionEntity>
{
    public void Configure(EntityTypeBuilder<RolePermissionEntity> builder)
    {
        builder.ToTable("role_permission");
        builder.Property(rolePermission => rolePermission.Permission).HasMaxLength(100);
        builder.HasIndex(rolePermission => new { rolePermission.RoleId, rolePermission.Permission }).IsUnique().HasFilter("[DeletedAt] IS NULL");
    }
}
