using AspNetCoreSkeletonApplication.Common.Account.Dtos;
using AspNetCoreSkeletonApplication.Common.Account.Persistence.Services;

namespace AspNetCoreSkeletonApplication.Common.Account.Services.Biz;

/// <summary>다른 도메인(인가의 그룹 소속 등)이 계정을 볼 때 거치는 입구.</summary>
public class UserAccountBizService(IUserAccountEntityService userAccountEntityService)
{
    public Task<UserAccountDto> GetAsync(long id, CancellationToken cancellationToken) =>
        userAccountEntityService.GetAsync(id, cancellationToken);

    public Task<IReadOnlyList<UserAccountDto>> ListByIdsAsync(IReadOnlyCollection<long> ids, CancellationToken cancellationToken) =>
        userAccountEntityService.ListByIdsAsync(ids, cancellationToken);
}
