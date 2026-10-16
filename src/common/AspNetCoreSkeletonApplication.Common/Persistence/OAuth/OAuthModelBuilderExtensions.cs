using Microsoft.EntityFrameworkCore;
using OpenIddict.EntityFrameworkCore.Models;

namespace AspNetCoreSkeletonApplication.Common.Persistence.OAuth;

internal static class OAuthModelBuilderExtensions
{
    /// <summary>
    /// OAuth 서버(OpenIddict)의 저장소 — 클라이언트 · 동의(authorization) · scope · 토큰. 키는 <c>long</c>, 테이블은 <c>oauth_</c> 접두사다
    /// (프레임워크가 정의한 엔티티 — 업무 테이블 규칙의 대상이 아니다).
    /// </summary>
    public static ModelBuilder ConfigureOAuthTables(this ModelBuilder modelBuilder)
    {
        modelBuilder.UseOpenIddict<long>();
        modelBuilder.Entity<OpenIddictEntityFrameworkCoreApplication<long>>().ToTable("oauth_application");
        modelBuilder.Entity<OpenIddictEntityFrameworkCoreAuthorization<long>>().ToTable("oauth_authorization");
        modelBuilder.Entity<OpenIddictEntityFrameworkCoreScope<long>>().ToTable("oauth_scope");
        modelBuilder.Entity<OpenIddictEntityFrameworkCoreToken<long>>().ToTable("oauth_token");
        return modelBuilder;
    }
}
