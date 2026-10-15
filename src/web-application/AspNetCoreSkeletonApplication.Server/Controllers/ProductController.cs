using AspNetCoreSkeletonApplication.Server.Models;
using AspNetCoreSkeletonApplication.Server.Services.Mapping;
using AspNetCoreSkeletonApplication.Common.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AspNetCoreSkeletonApplication.Server.Controllers;

/// <summary>
/// DB 를 거치는 전 계층 샘플. 상태 전이 규칙은 Common 의 Domain 이 정한다. 조회는 익명, 생성은 로그인(기본 정책),
/// 상태 변경 · 삭제는 관리자 정책이다.
/// </summary>
[ApiController]
[Route("api/products")]
public class ProductController(ProductMappingService mappingService) : ControllerBase
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
    [Authorize(Policy = Policies.Administrator)]
    public Task<ProductModel> ChangeStatus(long id, ProductStatusChangeModel model, CancellationToken cancellationToken) =>
        mappingService.ChangeStatusAsync(id, model, cancellationToken);

    [HttpDelete("{id:long}")]
    [Authorize(Policy = Policies.Administrator)]
    public async Task<NoContentResult> Delete(long id, CancellationToken cancellationToken)
    {
        await mappingService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:long}/status-history")]
    [AllowAnonymous]
    public Task<IReadOnlyList<ProductStatusHistoryModel>> ListStatusHistory(long id, CancellationToken cancellationToken) =>
        mappingService.ListStatusHistoryAsync(id, cancellationToken);
}
