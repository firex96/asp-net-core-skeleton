using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace AspNetCoreSkeletonApplication.Server.Product.Authorization;

/// <summary>상품 하나에 대한 동작 — <c>IAuthorizationService.AuthorizeAsync(User, product, ProductOperations.Delete)</c>.</summary>
public static class ProductOperations
{
    public static readonly OperationAuthorizationRequirement Delete = new() { Name = nameof(Delete) };
}
