using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AspNetCoreSkeletonApplication.Common.Persistence.Identity;

internal static class IdentityModelBuilderExtensions
{
    /// <summary>
    /// Identity 테이블에 <c>identity_</c> 접두사를 붙인다 — 프레임워크 소유임이 이름에 드러나고 SQL Server 예약어(<c>user</c>)와 겹치지 않는다.
    /// 역할 테이블은 없다 — 역할은 인가 도메인(<c>user_group</c> · <c>role</c> …)이 맡는다.
    /// </summary>
    public static ModelBuilder ConfigureIdentityTables(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ApplicationUser>().ToTable("identity_user");
        modelBuilder.Entity<IdentityUserClaim<long>>().ToTable("identity_user_claim");
        modelBuilder.Entity<IdentityUserLogin<long>>().ToTable("identity_user_login");
        modelBuilder.Entity<IdentityUserToken<long>>().ToTable("identity_user_token");
        return modelBuilder;
    }
}
