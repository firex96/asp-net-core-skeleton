using AspNetCoreSkeletonApplication.Common.Account.Services.Biz;
using AspNetCoreSkeletonApplication.Server.Account.Mappers;
using AspNetCoreSkeletonApplication.Server.Account.Models;

namespace AspNetCoreSkeletonApplication.Server.Account.Services.Mapping;

public class UserAccountMappingService(UserAccountBizService userAccountBizService)
{
    public async Task<UserAccountModel> GetByEmailAsync(string email, CancellationToken cancellationToken) =>
        (await userAccountBizService.GetByEmailAsync(email, cancellationToken)).ToModel();
}
