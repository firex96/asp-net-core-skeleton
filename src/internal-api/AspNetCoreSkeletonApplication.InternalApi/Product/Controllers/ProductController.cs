using AspNetCoreSkeletonApplication.InternalApi.Product.Models;
using AspNetCoreSkeletonApplication.InternalApi.Product.Services.Mapping;
using Microsoft.AspNetCore.Mvc;

namespace AspNetCoreSkeletonApplication.InternalApi.Product.Controllers;

[ApiController]
[Route("api/products")]
public class ProductController(ProductMappingService mappingService) : ControllerBase
{
    [HttpGet("{id:long}")]
    public Task<ProductModel> Get(long id, CancellationToken cancellationToken) =>
        mappingService.GetAsync(id, cancellationToken);
}
