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
        var response = await ManagerClient().PostAsJsonAsync(
            "/api/products", new { name = "Keyboard", price = 49000 }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var product = await ReadJsonAsync(response);
        Assert.Equal("Draft", product.GetProperty("status").GetString());
        Assert.Equal($"/api/products/{product.GetProperty("id").GetInt64()}", response.Headers.Location?.AbsolutePath);
    }

    [Fact]
    public async Task Get_Missing_Is404ProblemDetails()
    {
        var response = await ManagerClient().GetAsync("/api/products/999999", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("NOT_FOUND_ERROR", (await ReadJsonAsync(response)).GetProperty("code").GetString());
    }

    [Theory]
    [InlineData("""{"price": 1000}""")]
    [InlineData("""{"name": "Keyboard"}""")]
    [InlineData("""{"name": "Keyboard", "price": -1}""")]
    public async Task Create_InvalidBody_Is400NotValid(string body)
    {
        var response = await ManagerClient().PostAsync(
            "/api/products", new StringContent(body, System.Text.Encoding.UTF8, "application/json"), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("NOT_VALID_ERROR", (await ReadJsonAsync(response)).GetProperty("code").GetString());
    }

    [Fact]
    public async Task ChangeStatus_FollowsDomainRule()
    {
        var client = ManagerClient();
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
    public async Task StatusHistory_RecordsOnlyAppliedChanges()
    {
        var client = ManagerClient();
        var id = (await ReadJsonAsync(await client.PostAsJsonAsync(
            "/api/products", new { name = "Stand", price = 29000 }, TestContext.Current.CancellationToken))).GetProperty("id").GetInt64();

        await client.PutAsJsonAsync($"/api/products/{id}/status", new { status = "OnSale" }, TestContext.Current.CancellationToken);
        await client.PutAsJsonAsync($"/api/products/{id}/status", new { status = "Draft" }, TestContext.Current.CancellationToken); // 409 — 기록되지 않는다

        var history = await ReadJsonAsync(await client.GetAsync($"/api/products/{id}/status-history", TestContext.Current.CancellationToken));
        var entry = Assert.Single(history.EnumerateArray());
        Assert.Equal("Draft", entry.GetProperty("fromStatus").GetString());
        Assert.Equal("OnSale", entry.GetProperty("toStatus").GetString());
    }

    [Fact]
    public async Task Delete_Is204ThenGetIs404()
    {
        var client = ManagerClient();
        var id = (await ReadJsonAsync(await client.PostAsJsonAsync(
            "/api/products", new { name = "Tray", price = 5000 }, TestContext.Current.CancellationToken))).GetProperty("id").GetInt64();

        var deleted = await client.DeleteAsync($"/api/products/{id}", TestContext.Current.CancellationToken);
        var again = await client.DeleteAsync($"/api/products/{id}", TestContext.Current.CancellationToken);
        var fetched = await client.GetAsync($"/api/products/{id}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, again.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, fetched.StatusCode);
    }

    [Fact]
    public async Task StatusHistory_MissingProduct_Is404()
    {
        var response = await ManagerClient().GetAsync("/api/products/777777/status-history", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Response_DoesNotExposeAuditorIds()
    {
        var response = await ManagerClient().PostAsJsonAsync(
            "/api/products", new { name = "Pad", price = 9000 }, TestContext.Current.CancellationToken);

        var product = await ReadJsonAsync(response);
        Assert.False(product.TryGetProperty("createdBy", out _));
        Assert.True(product.TryGetProperty("createdAt", out _));
    }

    private HttpClient ManagerClient()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.HeaderName, "1;products.manage");
        return client;
    }

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).RootElement;
}
