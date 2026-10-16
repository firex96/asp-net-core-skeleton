using AspNetCoreSkeletonApplication.Common.Authorization.Persistence.Entities;
using AspNetCoreSkeletonApplication.Common.Persistence.Conventions;
using AspNetCoreSkeletonApplication.Common.Mail.Persistence.Entities;
using AspNetCoreSkeletonApplication.Common.Product.Persistence.Entities;
using AspNetCoreSkeletonApplication.Common.Persistence.Identity;
using AspNetCoreSkeletonApplication.Common.Persistence.OAuth;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

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

    /// <summary>
    /// Identity 스키마 3판(패스키 포함)을 앱 서비스 없이 만든 컨텍스트(테스트 · 도구)에도 준다. Identity 는 판을 앱 서비스의
    /// <c>IdentityOptions.Stores.SchemaVersion</c> 에서 읽어서, 앱 서비스가 없으면 1판 모델이 된다 — 마이그레이션과 어긋나고, 먼저 만든 모델이
    /// 프로세스 안에서 캐시되므로 앱 컨텍스트까지 1판을 쓰게 될 수 있다.
    /// </summary>
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (optionsBuilder.Options.FindExtension<CoreOptionsExtension>()?.ApplicationServiceProvider is null)
        {
            optionsBuilder.UseApplicationServiceProvider(IdentitySchemaServices);
        }
    }

    /// <summary>
    /// Identity 저장소 설정 — 앱(<c>AddCommon</c>)과 앱 서비스 없는 컨텍스트가 함께 쓴다. 스키마 3판(패스키)이고, 키 칼럼 길이는
    /// 1판 그대로(450) 둔다 — 2판부터의 기본값(128)으로 줄이면 기본 키 칼럼을 바꾸는 마이그레이션이 생긴다.
    /// </summary>
    public static void ConfigureIdentityStore(StoreOptions stores)
    {
        stores.SchemaVersion = IdentitySchemaVersions.Version3;
        stores.MaxLengthForKeys = 450;
    }

    private static readonly IServiceProvider IdentitySchemaServices = new ServiceCollection()
        .Configure<IdentityOptions>(options => ConfigureIdentityStore(options.Stores))
        .BuildServiceProvider();
}
