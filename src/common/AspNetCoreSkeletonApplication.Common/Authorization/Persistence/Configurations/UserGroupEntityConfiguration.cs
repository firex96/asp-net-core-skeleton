using AspNetCoreSkeletonApplication.Common.Authorization.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AspNetCoreSkeletonApplication.Common.Authorization.Persistence.Configurations;

internal sealed class UserGroupEntityConfiguration : IEntityTypeConfiguration<UserGroupEntity>
{
    public void Configure(EntityTypeBuilder<UserGroupEntity> builder)
    {
        builder.ToTable("user_group");
        builder.Property(group => group.Name).HasMaxLength(100);
        builder.Property(group => group.Description).HasMaxLength(500);
        // 지운 그룹과 같은 이름을 다시 쓸 수 있게 살아 있는 행끼리만 유일하다.
        builder.HasIndex(group => group.Name).IsUnique().HasFilter("[DeletedAt] IS NULL");
    }
}
