using System.Net;
using System.Text.Json;
using AspNetCoreSkeletonApplication.Common.Product.Dtos;
using AspNetCoreSkeletonApplication.Common.Product.Enums;
using AspNetCoreSkeletonApplication.Common.Product.Persistence.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AspNetCoreSkeletonApplication.FunctionalTests.InternalApi;

/// <summary>InternalApi 도 Common 의 웹 구성(에러 응답 · 추적 ID)을 그대로 쓰고, 같은 Dto 를 자기 Model 로 노출한다.</summary>
public class InternalApiProductTests(InternalApiWebApplicationFactory factory) : IClassFixture<InternalApiWebApplicationFactory>
{
    [Fact]
    public async Task Get_Existing_ExposesAuditorIds()
    {
        var product = await factory.Services.GetRequiredService<IProductEntityService>()
            .CreateAsync(new ProductCreateDto("Cable", 3000m), ProductStatus.Draft, TestContext.Current.CancellationToken);

        var response = await factory.CreateClient().GetAsync($"/api/products/{product.Id}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ReadJsonAsync(response);
        Assert.Equal("Cable", body.GetProperty("name").GetString());
        Assert.True(body.TryGetProperty("createdBy", out _));
    }

    [Fact]
    public async Task Get_Missing_Is404ProblemDetailsWithTraceId()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/products/987654");
        request.Headers.Add("X-Trace-Id", "internal-trace-1");

        var response = await factory.CreateClient().SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await ReadJsonAsync(response);
        Assert.Equal("NOT_FOUND_ERROR", problem.GetProperty("code").GetString());
        Assert.Equal("internal-trace-1", problem.GetProperty("traceId").GetString());
    }

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).RootElement;
}
