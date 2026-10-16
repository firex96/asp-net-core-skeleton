using AspNetCoreSkeletonApplication.Common.Account.Dtos;
using AspNetCoreSkeletonApplication.Common.Account.Persistence.Services;
using AspNetCoreSkeletonApplication.Common.Errors;

namespace AspNetCoreSkeletonApplication.Common.Account.Services.Biz;

/// <summary>다른 도메인(인가의 그룹 소속 등)이 계정을 볼 때 거치는 입구.</summary>
public class UserAccountBizService(IUserAccountEntityService userAccountEntityService)
{
    public Task<UserAccountDto> GetAsync(long id, CancellationToken cancellationToken) =>
        userAccountEntityService.GetAsync(id, cancellationToken);

    public Task<IReadOnlyList<UserAccountDto>> ListByIdsAsync(IReadOnlyCollection<long> ids, CancellationToken cancellationToken) =>
        userAccountEntityService.ListByIdsAsync(ids, cancellationToken);

    /// <summary>관리 화면이 이메일로 사람을 찾는다(그룹에 넣기). 없으면 <c>ItemNotFoundException</c>(404).</summary>
    public async Task<UserAccountDto> GetByEmailAsync(string email, CancellationToken cancellationToken) =>
        await userAccountEntityService.FindByEmailAsync(email, cancellationToken)
        ?? throw new ItemNotFoundException("그 이메일로 가입한 사용자가 없어요.");
}
