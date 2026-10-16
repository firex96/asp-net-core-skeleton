using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AspNetCoreSkeletonApplication.Common.Product.Enums;
using AspNetCoreSkeletonApplication.Common.Persistence.Context;
using AspNetCoreSkeletonApplication.IntegrationTests.Support;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AspNetCoreSkeletonApplication.IntegrationTests;

/// <summary>
/// 실제 SQL Server 에 마이그레이션을 적용하고 Product API 를 끝까지 통과시킨다 — EntityService 의 쿼리 · 매핑,
/// 트랜잭션, 로그인한 사용자가 남는 감사 필드를 대역 없이 확인한다.
/// </summary>
[Collection(InfrastructureCollection.Name)]
public class ProductApiTests(InfrastructureFixture infrastructure) : IAsyncLifetime
{
    private const string Database = "product_api";

    private WebApplicationFactory<Program> _factory = null!;

    public async ValueTask InitializeAsync()
    {
        await infrastructure.RecreateDatabaseAsync(Database);
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => infrastructure.ConfigureApp(builder, Database));
    }

    public async ValueTask DisposeAsync() => await _factory.DisposeAsync();

    [Fact]
    public async Task CreateThenGet_RoundTripsThroughDatabase()
    {
        var (user, _) = await TestUsers.SignInAsync(_factory, "creator@example.com", administrator: false);

        var id = await CreateAsync(user, "Keyboard", 49000.50m);
        var fetched = await ReadJsonAsync(await user.GetAsync($"/api/products/{id}", TestContext.Current.CancellationToken)); // 판매 전이라 작성자가 읽는다

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
    public async Task ChangeStatus_RecordsCreatorAndUpdater()
    {
        var (user, userId) = await TestUsers.SignInAsync(_factory, "maker@example.com", administrator: false);
        var (admin, adminId) = await TestUsers.SignInAsync(_factory, "admin@example.com", administrator: true);
        var id = await CreateAsync(user, "Pad", 9000);

        var response = await admin.PutAsJsonAsync($"/api/products/{id}/status", new { status = "OnSale" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var saved = await FindEntityAsync(id);
        Assert.Equal(ProductStatus.OnSale, saved.Status);
        Assert.Equal(userId, saved.CreatedBy);
        Assert.Equal(adminId, saved.UpdatedBy);
    }

    [Fact]
    public async Task Delete_IsLogical_RowStaysWithDeleter()
    {
        var (admin, adminId) = await TestUsers.SignInAsync(_factory, "deleter@example.com", administrator: true);
        var id = await CreateAsync(admin, "Shelf", 80000);

        var deleted = await admin.DeleteAsync($"/api/products/{id}", TestContext.Current.CancellationToken);
        var fetched = await admin.GetAsync($"/api/products/{id}", TestContext.Current.CancellationToken);
        var listed = await ReadJsonAsync(await admin.GetAsync("/api/products", TestContext.Current.CancellationToken));

        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, fetched.StatusCode);
        Assert.DoesNotContain(listed.EnumerateArray(), product => product.GetProperty("id").GetInt64() == id);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var row = await dbContext.Products.IgnoreQueryFilters().SingleAsync(product => product.Id == id, TestContext.Current.CancellationToken);
        Assert.NotNull(row.DeletedAt);
        Assert.Equal(adminId, row.DeletedBy);
        Assert.Equal(adminId, row.UpdatedBy);
    }

    [Fact]
    public async Task ChangeStatus_WritesHistoryInSameTransaction()
    {
        var (admin, adminId) = await TestUsers.SignInAsync(_factory, "historian@example.com", administrator: true);
        var id = await CreateAsync(admin, "Lamp", 15000);

        await admin.PutAsJsonAsync($"/api/products/{id}/status", new { status = "Discontinued" }, TestContext.Current.CancellationToken);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var history = await dbContext.ProductStatusHistories.SingleAsync(row => row.ProductId == id, TestContext.Current.CancellationToken);
        Assert.Equal(ProductStatus.Draft, history.FromStatus);
        Assert.Equal(ProductStatus.Discontinued, history.ToStatus);
        Assert.Equal(adminId, history.CreatedBy);
    }

    private static async Task<long> CreateAsync(HttpClient client, string name, decimal price)
    {
        var response = await client.PostAsJsonAsync("/api/products", new { name, price }, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        return (await ReadJsonAsync(response)).GetProperty("id").GetInt64();
    }

    private async Task<(long CreatedBy, long UpdatedBy, ProductStatus Status)> FindEntityAsync(long id)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var entity = await dbContext.Products.SingleAsync(product => product.Id == id, TestContext.Current.CancellationToken);
        return (entity.CreatedBy, entity.UpdatedBy, entity.Status);
    }

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).RootElement;
}
