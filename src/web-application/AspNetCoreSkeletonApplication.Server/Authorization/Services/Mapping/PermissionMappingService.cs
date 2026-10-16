using AspNetCoreSkeletonApplication.Common.Authorization.Services.Biz;
using AspNetCoreSkeletonApplication.Server.Authorization.Mappers;
using AspNetCoreSkeletonApplication.Server.Authorization.Models;

namespace AspNetCoreSkeletonApplication.Server.Authorization.Services.Mapping;

public class PermissionMappingService(PermissionBizService permissionBizService)
{
    public IReadOnlyList<PermissionModel> ListCatalog() => permissionBizService.ListCatalog().ToModels();
}
