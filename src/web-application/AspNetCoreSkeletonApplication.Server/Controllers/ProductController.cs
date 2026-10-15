using AspNetCoreSkeletonApplication.Server.Models;
using AspNetCoreSkeletonApplication.Server.Services.Mapping;
using Microsoft.AspNetCore.Mvc;

namespace AspNetCoreSkeletonApplication.Server.Controllers;

/// <summary>DB 를 거치는 전 계층 샘플. 상태 전이 규칙은 Common 의 Domain 이 정한다.</summary>
[ApiController]
[Route("api/products")]
public class ProductController(ProductMappingService mappingService) : ControllerBase
{
    [HttpGet("{id:long}")]
    public Task<ProductModel> Get(long id, CancellationToken cancellationToken) =>
        mappingService.GetAsync(id, cancellationToken);

    [HttpGet]
    public Task<IReadOnlyList<ProductModel>> List(CancellationToken cancellationToken) =>
        mappingService.ListAsync(cancellationToken);

    [HttpPost]
    public async Task<ActionResult<ProductModel>> Create(ProductCreateModel model, CancellationToken cancellationToken)
    {
        var created = await mappingService.CreateAsync(model, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:long}/status")]
    public Task<ProductModel> ChangeStatus(long id, ProductStatusChangeModel model, CancellationToken cancellationToken) =>
        mappingService.ChangeStatusAsync(id, model, cancellationToken);
}
