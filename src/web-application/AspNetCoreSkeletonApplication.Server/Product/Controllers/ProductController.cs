using AspNetCoreSkeletonApplication.Common.Authorization;
using AspNetCoreSkeletonApplication.Common.Web.Authorization;
using AspNetCoreSkeletonApplication.Server.Product.Authorization;
using AspNetCoreSkeletonApplication.Server.Product.Models;
using AspNetCoreSkeletonApplication.Server.Product.Services.Mapping;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AspNetCoreSkeletonApplication.Server.Product.Controllers;

/// <summary>
/// DB 를 거치는 전 계층 샘플. 상태 전이 규칙은 Common 의 Domain 이 정한다. 조회는 익명이지만 판매 전(Draft) 상품은 작성자와
/// 상품 관리자만 본다(목록에서 빠지고 단건 · 이력은 404). 생성은 로그인(기본 정책), 상태 변경은 권한 <c>products.manage</c>,
/// 삭제는 리소스 판정(권한이 있거나 자기 Draft 상품)이다. 권한은 언제나 권한 정책으로 판정한다 — Bearer 토큰은 사용자 권한 ∩ scope 다.
/// </summary>
[ApiController]
[Route("api/products")]
public class ProductController(ProductMappingService mappingService, IAuthorizationService authorizationService) : ControllerBase
{
    [HttpGet("{id:long}")]
    [AllowAnonymous]
    public async Task<ProductModel> Get(long id, CancellationToken cancellationToken) =>
        await mappingService.GetAsync(id, await ViewerAsync(), cancellationToken);

    [HttpGet]
    [AllowAnonymous]
    public async Task<IReadOnlyList<ProductModel>> List(CancellationToken cancellationToken) =>
        await mappingService.ListAsync(await ViewerAsync(), cancellationToken);

    [HttpPost]
    public async Task<ActionResult<ProductModel>> Create(ProductCreateModel model, CancellationToken cancellationToken)
    {
        var created = await mappingService.CreateAsync(model, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:long}/status")]
    [HasPermission(Permissions.Products.Manage)]
    public Task<ProductModel> ChangeStatus(long id, ProductStatusChangeModel model, CancellationToken cancellationToken) =>
        mappingService.ChangeStatusAsync(id, model, cancellationToken);

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        var product = await mappingService.GetAsync(id, await ViewerAsync(), cancellationToken);
        if (!(await authorizationService.AuthorizeAsync(User, product, ProductOperations.Delete)).Succeeded)
        {
            return Forbid();
        }

        await mappingService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:long}/status-history")]
    [AllowAnonymous]
    public async Task<IReadOnlyList<ProductStatusHistoryModel>> ListStatusHistory(long id, CancellationToken cancellationToken) =>
        await mappingService.ListStatusHistoryAsync(id, await ViewerAsync(), cancellationToken);

    /// <summary>요청한 사람 — 상품 관리 권한은 권한 정책(→ RequestPermissions → 평가기)으로 판정한다. 익명이면 권한 없음이다.</summary>
    private async Task<ProductViewerModel> ViewerAsync() =>
        new(UserClaims.IdOf(User),
            (await authorizationService.AuthorizeAsync(User, HasPermissionAttribute.PolicyNameOf(Permissions.Products.Manage))).Succeeded);
}
