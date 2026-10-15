using AspNetCoreSkeletonApplication.Common.Constants;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AspNetCoreSkeletonApplication.Common.Persistence.Identity;

internal static class IdentityModelBuilderExtensions
{
    /// <summary>
    /// Identity 테이블에 <c>identity_</c> 접두사를 붙인다 — 프레임워크 소유임이 이름에 드러나고 SQL Server 예약어(<c>user</c>)와 겹치지 않는다.
    /// 역할 <see cref="Roles.Administrator"/> 를 시드한다. 관리자 지정은 README 「인증」 참고.
    /// </summary>
    public static ModelBuilder ConfigureIdentityTables(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ApplicationUser>().ToTable("identity_user");
        modelBuilder.Entity<ApplicationRole>().ToTable("identity_role");
        modelBuilder.Entity<IdentityUserRole<long>>().ToTable("identity_user_role");
        modelBuilder.Entity<IdentityUserClaim<long>>().ToTable("identity_user_claim");
        modelBuilder.Entity<IdentityUserLogin<long>>().ToTable("identity_user_login");
        modelBuilder.Entity<IdentityUserToken<long>>().ToTable("identity_user_token");
        modelBuilder.Entity<IdentityRoleClaim<long>>().ToTable("identity_role_claim");

        modelBuilder.Entity<ApplicationRole>().HasData(new ApplicationRole
        {
            Id = 1,
            Name = Roles.Administrator,
            NormalizedName = Roles.Administrator.ToUpperInvariant(),
            ConcurrencyStamp = "7c9e6679-7425-40de-944b-e07fc1f90ae7",
        });

        return modelBuilder;
    }
}
