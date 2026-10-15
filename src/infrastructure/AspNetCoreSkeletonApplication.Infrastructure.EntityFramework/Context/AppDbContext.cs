using Microsoft.EntityFrameworkCore;

namespace AspNetCoreSkeletonApplication.Infrastructure.EntityFramework.Context;

/// <summary>
/// 애플리케이션 DB 컨텍스트. 엔티티를 추가하면 여기에 <see cref="DbSet{TEntity}"/> 를 둔다.
/// 연결 문자열은 <c>ConnectionStrings:Default</c> 에서 Server 의 Program.cs 가 주입한다.
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options);
