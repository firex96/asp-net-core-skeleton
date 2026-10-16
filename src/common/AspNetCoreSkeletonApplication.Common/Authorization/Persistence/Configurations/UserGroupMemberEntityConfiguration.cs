using AspNetCoreSkeletonApplication.Common.Authorization.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AspNetCoreSkeletonApplication.Common.Authorization.Persistence.Configurations;

internal sealed class UserGroupMemberEntityConfiguration : IEntityTypeConfiguration<UserGroupMemberEntity>
{
    public void Configure(EntityTypeBuilder<UserGroupMemberEntity> builder)
    {
        builder.ToTable("user_group_member");
        builder.HasIndex(member => new { member.UserGroupId, member.UserId }).IsUnique().HasFilter("[DeletedAt] IS NULL");
        builder.HasIndex(member => member.UserId);
    }
}
