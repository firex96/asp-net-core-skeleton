using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AspNetCoreSkeletonApplication.Common.Constants;
using AspNetCoreSkeletonApplication.Common.Enums;
using AspNetCoreSkeletonApplication.Common.Persistence.Context;
using AspNetCoreSkeletonApplication.IntegrationTests.Support;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AspNetCoreSkeletonApplication.IntegrationTests;

/// <summary>
/// 실제 SQL Server 에 마이그레이션을 적용하고 Product API 를 끝까지 통과시킨다 — EntityService 의 쿼리 · 매핑,
/// 트랜잭션, 감사 필드 기록을 대역 없이 확인한다.
/// </summary>
[Collection(InfrastructureCollection.Name)]
public class ProductApiTests(InfrastructureFixture database) : IAsyncLifetime
{
    private WebApplicationFactory<Program> _factory = null!;

    public async ValueTask InitializeAsync()
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            database.ConfigureApp(builder, "product_api");
            builder.UseSetting("Audit:TrustUserHeader", "true");
        });

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.MigrateAsync();
    }

    public async ValueTask DisposeAsync() => await _factory.DisposeAsync();

    [Fact]
    public async Task CreateThenGet_RoundTripsThroughDatabase()
    {
        var client = _factory.CreateClient();

        var created = await ReadJsonAsync(await client.PostAsJsonAsync(
            "/api/products", new { name = "Keyboard", price = 49000.50m }, TestContext.Current.CancellationToken));
        var id = created.GetProperty("id").GetInt64();
        var fetched = await ReadJsonAsync(await client.GetAsync($"/api/products/{id}", TestContext.Current.CancellationToken));

        Assert.Equal("Keyboard", fetched.GetProperty("name").GetString());
        Assert.Equal(49000.50m, fetched.GetProperty("price").GetDecimal());
        Assert.Equal("Draft", fetched.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Get_Missing_Is404()
    {
        var response = await _factory.CreateClient().GetAsync("/api/products/424242", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithoutUserHeader_RecordsSystemAuditor()
    {
        var created = await ReadJsonAsync(await _factory.CreateClient().PostAsJsonAsync(
            "/api/products", new { name = "Mouse", price = 19000 }, TestContext.Current.CancellationToken));

        var saved = await FindEntityAsync(created.GetProperty("id").GetInt64());
        Assert.Equal(AuditorIds.System, saved.CreatedBy);
        Assert.NotEqual(default, saved.CreatedAt);
    }

    [Fact]
    public async Task ChangeStatus_WithTrustedUserHeader_RecordsUpdater()
    {
        var client = _factory.CreateClient();
        var created = await ReadJsonAsync(await client.PostAsJsonAsync(
            "/api/products", new { name = "Pad", price = 9000 }, TestContext.Current.CancellationToken));
        var id = created.GetProperty("id").GetInt64();

        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/products/{id}/status")
        {
            Content = JsonContent.Create(new { status = "OnSale" }),
        };
        request.Headers.Add("X-User-Id", "42");
        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var saved = await FindEntityAsync(id);
        Assert.Equal(ProductStatus.OnSale, saved.Status);
        Assert.Equal(AuditorIds.System, saved.CreatedBy);
        Assert.Equal(42, saved.UpdatedBy);
    }

    [Fact]
    public async Task Delete_IsLogical_RowStaysWithDeleter()
    {
        var client = _factory.CreateClient();
        var id = (await ReadJsonAsync(await client.PostAsJsonAsync(
            "/api/products", new { name = "Shelf", price = 80000 }, TestContext.Current.CancellationToken))).GetProperty("id").GetInt64();
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/products/{id}");
        request.Headers.Add("X-User-Id", "11");

        var deleted = await client.SendAsync(request, TestContext.Current.CancellationToken);
        var fetched = await client.GetAsync($"/api/products/{id}", TestContext.Current.CancellationToken);
        var listed = await ReadJsonAsync(await client.GetAsync("/api/products", TestContext.Current.CancellationToken));

        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, fetched.StatusCode);
        Assert.DoesNotContain(listed.EnumerateArray(), product => product.GetProperty("id").GetInt64() == id);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var row = await dbContext.Products.IgnoreQueryFilters().SingleAsync(product => product.Id == id, TestContext.Current.CancellationToken);
        Assert.NotNull(row.DeletedAt);
        Assert.Equal(11, row.DeletedBy);
        Assert.Equal(11, row.UpdatedBy);
    }

    [Fact]
    public async Task ChangeStatus_WritesHistoryInSameTransaction()
    {
        var client = _factory.CreateClient();
        var id = (await ReadJsonAsync(await client.PostAsJsonAsync(
            "/api/products", new { name = "Lamp", price = 15000 }, TestContext.Current.CancellationToken))).GetProperty("id").GetInt64();
        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/products/{id}/status") { Content = JsonContent.Create(new { status = "Discontinued" }) };
        request.Headers.Add("X-User-Id", "7");
        await client.SendAsync(request, TestContext.Current.CancellationToken);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var history = await dbContext.ProductStatusHistories.SingleAsync(row => row.ProductId == id, TestContext.Current.CancellationToken);
        Assert.Equal(ProductStatus.Draft, history.FromStatus);
        Assert.Equal(ProductStatus.Discontinued, history.ToStatus);
        Assert.Equal(7, history.CreatedBy);
    }

    private async Task<(long CreatedBy, DateTimeOffset CreatedAt, long UpdatedBy, ProductStatus Status)> FindEntityAsync(long id)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var entity = await dbContext.Products.SingleAsync(product => product.Id == id, TestContext.Current.CancellationToken);
        return (entity.CreatedBy, entity.CreatedAt, entity.UpdatedBy, entity.Status);
    }

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).RootElement;
}
