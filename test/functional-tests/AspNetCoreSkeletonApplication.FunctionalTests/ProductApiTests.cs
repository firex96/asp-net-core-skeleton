using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AspNetCoreSkeletonApplication.FunctionalTests.Support;

namespace AspNetCoreSkeletonApplication.FunctionalTests;

/// <summary>Product API 의 HTTP 계약 — 상태 코드, JSON 형태, 에러 응답 — 을 DB 없이 확인한다.</summary>
public class ProductApiTests(ProductWebApplicationFactory factory) : IClassFixture<ProductWebApplicationFactory>
{
    [Fact]
    public async Task Create_Returns201WithLocationAndDraftStatus()
    {
        var response = await factory.CreateClient().PostAsJsonAsync(
            "/api/products", new { name = "Keyboard", price = 49000 }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var product = await ReadJsonAsync(response);
        Assert.Equal("Draft", product.GetProperty("status").GetString());
        Assert.Equal($"/api/products/{product.GetProperty("id").GetInt64()}", response.Headers.Location?.AbsolutePath);
    }

    [Fact]
    public async Task Get_Missing_Is404ProblemDetails()
    {
        var response = await factory.CreateClient().GetAsync("/api/products/999999", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("NOT_FOUND_ERROR", (await ReadJsonAsync(response)).GetProperty("code").GetString());
    }

    [Theory]
    [InlineData("""{"price": 1000}""")]
    [InlineData("""{"name": "Keyboard"}""")]
    [InlineData("""{"name": "Keyboard", "price": -1}""")]
    public async Task Create_InvalidBody_Is400NotValid(string body)
    {
        var response = await factory.CreateClient().PostAsync(
            "/api/products", new StringContent(body, System.Text.Encoding.UTF8, "application/json"), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("NOT_VALID_ERROR", (await ReadJsonAsync(response)).GetProperty("code").GetString());
    }

    [Fact]
    public async Task ChangeStatus_FollowsDomainRule()
    {
        var client = factory.CreateClient();
        var created = await ReadJsonAsync(await client.PostAsJsonAsync(
            "/api/products", new { name = "Mouse", price = 19000 }, TestContext.Current.CancellationToken));
        var id = created.GetProperty("id").GetInt64();

        var discontinued = await client.PutAsJsonAsync(
            $"/api/products/{id}/status", new { status = "Discontinued" }, TestContext.Current.CancellationToken);
        var reopened = await client.PutAsJsonAsync(
            $"/api/products/{id}/status", new { status = "OnSale" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, discontinued.StatusCode);
        Assert.Equal("Discontinued", (await ReadJsonAsync(discontinued)).GetProperty("status").GetString());
        Assert.Equal(HttpStatusCode.Conflict, reopened.StatusCode);
        Assert.Equal("CONFLICT_ERROR", (await ReadJsonAsync(reopened)).GetProperty("code").GetString());
    }

    [Fact]
    public async Task Response_DoesNotExposeAuditorIds()
    {
        var response = await factory.CreateClient().PostAsJsonAsync(
            "/api/products", new { name = "Pad", price = 9000 }, TestContext.Current.CancellationToken);

        var product = await ReadJsonAsync(response);
        Assert.False(product.TryGetProperty("createdBy", out _));
        Assert.True(product.TryGetProperty("createdAt", out _));
    }

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).RootElement;
}
