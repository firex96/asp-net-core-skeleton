using AspNetCoreSkeletonApplication.Common.Account.Dtos;
using AspNetCoreSkeletonApplication.Server.Account.Models;
using Riok.Mapperly.Abstractions;

namespace AspNetCoreSkeletonApplication.Server.Account.Mappers;

[Mapper]
public static partial class UserAccountModelMapper
{
    public static partial UserAccountModel ToModel(this UserAccountDto dto);
}
