using Microsoft.AspNetCore.Authorization;

namespace AspNetCoreSkeletonApplication.Common.Web.Authorization;

public sealed record PermissionRequirement(string Permission) : IAuthorizationRequirement;
