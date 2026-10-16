namespace AspNetCoreSkeletonApplication.Server.Authorization.Models;

public sealed record RoleModel(long Id, string Name, string Description, IReadOnlyList<string> Permissions);
