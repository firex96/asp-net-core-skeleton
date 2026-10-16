using AspNetCoreSkeletonApplication.Common.Authorization.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AspNetCoreSkeletonApplication.Common.Authorization.Persistence.Configurations;

internal sealed class UserGroupRoleEntityConfiguration : IEntityTypeConfiguration<UserGroupRoleEntity>
{
    public void Configure(EntityTypeBuilder<UserGroupRoleEntity> builder)
    {
        builder.ToTable("user_group_role");
        builder.HasIndex(groupRole => new { groupRole.UserGroupId, groupRole.RoleId }).IsUnique().HasFilter("[DeletedAt] IS NULL");
    }
}
