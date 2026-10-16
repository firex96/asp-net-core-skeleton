using AspNetCoreSkeletonApplication.Common.Authorization;
using AspNetCoreSkeletonApplication.Common.Authorization.Services.Biz;
using AspNetCoreSkeletonApplication.Common.Product.Domain;
using AspNetCoreSkeletonApplication.Common.Web.Authorization;
using AspNetCoreSkeletonApplication.Server.Product.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace AspNetCoreSkeletonApplication.Server.Product.Authorization;

/// <summary>
/// 리소스 기반 인가 — 권한 코드만으로 정할 수 없는 판정(작성자 본인인가, 아직 Draft 인가)을 상품 데이터로 한다.
/// 권한 부분은 <see cref="IPermissionEvaluator"/> 에 묻고, 규칙 자체는 순수 Domain(<see cref="ProductAccessRule"/>)이 정한다.
/// </summary>
internal sealed class ProductAuthorizationHandler(IPermissionEvaluator evaluator)
    : AuthorizationHandler<OperationAuthorizationRequirement, ProductModel>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context, OperationAuthorizationRequirement requirement, ProductModel resource)
    {
        if (requirement != ProductOperations.Delete || UserClaims.IdOf(context.User) is not { } userId)
        {
            return;
        }

        var canManage = await evaluator.HasPermissionAsync(userId, Permissions.Products.Manage, CancellationToken.None);
        if (ProductAccessRule.CanDelete(resource.Status, resource.CreatedBy, userId, canManage))
        {
            context.Succeed(requirement);
        }
    }
}
