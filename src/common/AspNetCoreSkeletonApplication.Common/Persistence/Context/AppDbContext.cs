using AspNetCoreSkeletonApplication.Common.Persistence.Entities.Main;
using Microsoft.EntityFrameworkCore;

namespace AspNetCoreSkeletonApplication.Common.Persistence.Context;

/// <summary>
/// 애플리케이션 DB 컨텍스트. 엔티티를 추가하면 여기에 <see cref="DbSet{TEntity}"/> 를, <c>Configurations/</c> 에 매핑을 둔다.
/// internal 이다 — 앱과 BizService 는 DbContext 를 볼 수 없고 EntityService 를 거친다.
/// 등록은 <see cref="AspNetCoreSkeletonApplication.Common.Configuration.CommonServiceCollectionExtensions.AddCommon"/> 이 한다.
/// </summary>
internal sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<ProductEntity> Products => Set<ProductEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
}
