using AspNetCoreSkeletonApplication.Common.Persistence.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AspNetCoreSkeletonApplication.Common.Persistence.Identity;

/// <summary>
/// Identity 의 사용자 저장소. <see cref="AppDbContext"/> 는 조회 기본이 NoTracking 이라, 기본 저장소가 사용자 토큰(복구 코드 · 인증 앱 키 등)을 찾아
/// 값만 바꾸면 그 변경이 저장되지 않는다(<c>UPDATE</c> 가 나가지 않는다). 토큰을 찾는 조회만 추적 조회로 바꾼다 — 새로 넣기 · 지우기는 원래도 저장된다.
/// </summary>
internal sealed class ApplicationUserStore(AppDbContext context, IdentityErrorDescriber? describer = null)
    : UserOnlyStore<ApplicationUser, AppDbContext, long>(context, describer)
{
    protected override Task<IdentityUserToken<long>?> FindTokenAsync(
        ApplicationUser user, string loginProvider, string name, CancellationToken cancellationToken) =>
        Context.Set<IdentityUserToken<long>>()
            .AsTracking()
            .SingleOrDefaultAsync(token => token.UserId == user.Id && token.LoginProvider == loginProvider && token.Name == name, cancellationToken);
}
