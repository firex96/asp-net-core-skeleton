using AspNetCoreSkeletonApplication.Common.Authorization.Persistence.Entities;
using AspNetCoreSkeletonApplication.Common.Persistence.Conventions;
using AspNetCoreSkeletonApplication.Common.Mail.Persistence.Entities;
using AspNetCoreSkeletonApplication.Common.Product.Persistence.Entities;
using AspNetCoreSkeletonApplication.Common.Persistence.Identity;
using AspNetCoreSkeletonApplication.Common.Persistence.OAuth;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AspNetCoreSkeletonApplication.Common.Persistence.Context;

/// <summary>
/// 애플리케이션 DB 컨텍스트. 엔티티를 추가하면 여기에 <see cref="DbSet{TEntity}"/> 를, 그 도메인의 <c>Persistence/Configurations/</c> 에 매핑을 둔다.
/// internal 이다 — 앱과 BizService 는 DbContext 를 볼 수 없고 EntityService 를 거친다. 로그인 계정(Identity) 테이블도 함께 갖는다 — 역할은 Identity 가 아니라 인가 도메인(그룹 · 역할 · 권한)이 맡으므로 <see cref="IdentityUserContext{TUser, TKey}"/> 다.
/// 등록은 <see cref="AspNetCoreSkeletonApplication.Common.Configuration.CommonServiceCollectionExtensions.AddCommon"/> 이 한다.
/// </summary>
internal sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityUserContext<ApplicationUser, long>(options)
{
    public DbSet<UserGroupEntity> UserGroups => Set<UserGroupEntity>();

    public DbSet<UserGroupMemberEntity> UserGroupMembers => Set<UserGroupMemberEntity>();

    public DbSet<RoleEntity> Roles => Set<RoleEntity>();

    public DbSet<UserGroupRoleEntity> UserGroupRoles => Set<UserGroupRoleEntity>();

    public DbSet<RolePermissionEntity> RolePermissions => Set<RolePermissionEntity>();

    public DbSet<MailOutboxEntity> MailOutbox => Set<MailOutboxEntity>();

    public DbSet<ProductEntity> Products => Set<ProductEntity>();

    public DbSet<ProductStatusHistoryEntity> ProductStatusHistories => Set<ProductStatusHistoryEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly)
            .ConfigureIdentityTables()
            .ConfigureOAuthTables()
            .UseStringEnumCodes()
            .UseSoftDeleteFilter();
    }
}
