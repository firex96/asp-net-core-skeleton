using System.Net.Http.Json;
using System.Text.Json;
using AspNetCoreSkeletonApplication.Common.Persistence.Context;
using AspNetCoreSkeletonApplication.IntegrationTests.Support;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AspNetCoreSkeletonApplication.IntegrationTests;

/// <summary>
/// EntityService 의 캐시를 실제 Redis · SQL Server 로 확인한다 — DB 를 몰래 바꿔도 캐시된 값이 나오고(적중),
/// 변경 API 를 거치면 커밋 뒤에 태그가 무효화되어 새 값이 나오며(무효화), 다른 인스턴스도 Redis(L2) 로 같은 값을 본다.
/// </summary>
[Collection(InfrastructureCollection.Name)]
public class ProductCacheTests(InfrastructureFixture infrastructure) : IAsyncLifetime
{
    private const string Database = "product_cache";

    private WebApplicationFactory<Program> _first = null!;
    private WebApplicationFactory<Program> _second = null!;

    public async ValueTask InitializeAsync()
    {
        _first = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => infrastructure.ConfigureApp(builder, Database));
        _second = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => infrastructure.ConfigureApp(builder, Database));

        using var scope = _first.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.MigrateAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _second.DisposeAsync();
        await _first.DisposeAsync();
    }

    [Fact]
    public async Task Get_IsServedFromCache_UntilAChangeInvalidatesIt()
    {
        var client = _first.CreateClient();
        var id = await CreateAsync(client, "Original");
        Assert.Equal("Original", await GetNameAsync(client, id)); // 캐시에 들어간다

        await RenameBehindTheCacheAsync(id, "ChangedInDb");
        Assert.Equal("Original", await GetNameAsync(client, id)); // 적중 — DB 를 보지 않았다

        await client.PutAsJsonAsync($"/api/products/{id}/status", new { status = "OnSale" }, TestContext.Current.CancellationToken);
        Assert.Equal("ChangedInDb", await GetNameAsync(client, id)); // 변경이 태그를 무효화했다
    }

    [Fact]
    public async Task List_IsInvalidatedByCreate()
    {
        var client = _first.CreateClient();
        var before = await CountAsync(client);

        await CreateAsync(client, "Added");

        Assert.Equal(before + 1, await CountAsync(client));
    }

    [Fact]
    public async Task AnotherInstance_ReadsTheSameEntryFromRedis()
    {
        var id = await CreateAsync(_first.CreateClient(), "Shared");
        Assert.Equal("Shared", await GetNameAsync(_first.CreateClient(), id)); // 첫 인스턴스가 L1 · L2 에 넣는다

        await RenameBehindTheCacheAsync(id, "ChangedInDb");

        Assert.Equal("Shared", await GetNameAsync(_second.CreateClient(), id)); // 둘째 인스턴스는 L1 이 비어 있어 Redis 에서 읽는다
    }

    private async Task RenameBehindTheCacheAsync(long id, string name)
    {
        using var scope = _first.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Products.Where(product => product.Id == id)
            .ExecuteUpdateAsync(setters => setters.SetProperty(product => product.Name, name), TestContext.Current.CancellationToken);
    }

    private static async Task<long> CreateAsync(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync("/api/products", new { name, price = 1000 }, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).RootElement.GetProperty("id").GetInt64();
    }

    private static async Task<string?> GetNameAsync(HttpClient client, long id) =>
        JsonDocument.Parse(await client.GetStringAsync($"/api/products/{id}", TestContext.Current.CancellationToken))
            .RootElement.GetProperty("name").GetString();

    private static async Task<int> CountAsync(HttpClient client) =>
        JsonDocument.Parse(await client.GetStringAsync("/api/products", TestContext.Current.CancellationToken)).RootElement.GetArrayLength();
}
