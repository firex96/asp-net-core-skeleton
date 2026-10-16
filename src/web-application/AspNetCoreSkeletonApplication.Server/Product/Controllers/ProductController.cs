using AspNetCoreSkeletonApplication.Common.Authorization;
using AspNetCoreSkeletonApplication.Common.Web.Authorization;
using AspNetCoreSkeletonApplication.Server.Product.Authorization;
using AspNetCoreSkeletonApplication.Server.Product.Models;
using AspNetCoreSkeletonApplication.Server.Product.Services.Mapping;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AspNetCoreSkeletonApplication.Server.Product.Controllers;

/// <summary>
/// DB 를 거치는 전 계층 샘플. 상태 전이 규칙은 Common 의 Domain 이 정한다. 조회는 익명, 생성은 로그인(기본 정책),
/// 상태 변경은 권한 <c>products.manage</c>, 삭제는 리소스 판정(권한이 있거나 자기 Draft 상품)이다.
/// </summary>
[ApiController]
[Route("api/products")]
public class ProductController(ProductMappingService mappingService, IAuthorizationService authorizationService) : ControllerBase
{
    [HttpGet("{id:long}")]
    [AllowAnonymous]
    public Task<ProductModel> Get(long id, CancellationToken cancellationToken) =>
        mappingService.GetAsync(id, cancellationToken);

    [HttpGet]
    [AllowAnonymous]
    public Task<IReadOnlyList<ProductModel>> List(CancellationToken cancellationToken) =>
        mappingService.ListAsync(cancellationToken);

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
        var product = await mappingService.GetAsync(id, cancellationToken);
        if (!(await authorizationService.AuthorizeAsync(User, product, ProductOperations.Delete)).Succeeded)
        {
            return Forbid();
        }

        await mappingService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:long}/status-history")]
    [AllowAnonymous]
    public Task<IReadOnlyList<ProductStatusHistoryModel>> ListStatusHistory(long id, CancellationToken cancellationToken) =>
        mappingService.ListStatusHistoryAsync(id, cancellationToken);
}
