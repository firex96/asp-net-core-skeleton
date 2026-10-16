using System.Net.Http.Json;
using System.Text.Json;
using AspNetCoreSkeletonApplication.Common.Persistence.Context;
using AspNetCoreSkeletonApplication.IntegrationTests.Support;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace AspNetCoreSkeletonApplication.IntegrationTests;

/// <summary>
/// EntityService 의 캐시를 실제 Redis · SQL Server 로 확인한다 — DB 를 몰래 바꿔도 캐시된 값이 나오고(적중),
/// 변경 API 를 거치면 커밋 뒤에 태그가 무효화되어 새 값이 나오며(무효화), 다른 인스턴스도 Redis(L2) 로 같은 값을 보고,
/// 한 인스턴스의 변경은 백플레인으로 다른 인스턴스의 메모리(L1)에서도 곧바로 지워진다.
/// </summary>
[Collection(InfrastructureCollection.Name)]
public class ProductCacheTests(InfrastructureFixture infrastructure) : IAsyncLifetime
{
    private const string Database = "product_cache";

    private WebApplicationFactory<Program> _first = null!;
    private WebApplicationFactory<Program> _second = null!;

    public async ValueTask InitializeAsync()
    {
        await infrastructure.RecreateDatabaseAsync(Database);
        _first = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => infrastructure.ConfigureApp(builder, Database));
        _second = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => infrastructure.ConfigureApp(builder, Database));
    }

    public async ValueTask DisposeAsync()
    {
        await _second.DisposeAsync();
        await _first.DisposeAsync();
    }

    [Fact]
    public async Task Get_IsServedFromCache_UntilAChangeInvalidatesIt()
    {
        var (client, _) = await TestUsers.SignInAsync(_first, "cache-admin@example.com", administrator: true);
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
        var (client, _) = await TestUsers.SignInAsync(_first, "cache-user@example.com", administrator: false);
        var before = await CountAsync(client);

        await CreateAsync(client, "Added");

        Assert.Equal(before + 1, await CountAsync(client));
    }

    [Fact]
    public async Task AnotherInstance_ReadsTheSameEntryFromRedis()
    {
        // 판매 전 상품이라 작성자 · 관리자만 본다 — 둘째 인스턴스에서는 관리자로 읽는다.
        var (client, _) = await TestUsers.SignInAsync(_first, "cache-shared@example.com", administrator: false);
        var (secondManager, _) = await TestUsers.SignInAsync(_second, "cache-shared-manager@example.com", administrator: true);
        var id = await CreateAsync(client, "Shared");
        Assert.Equal("Shared", await GetNameAsync(client, id)); // 첫 인스턴스가 L1 · L2 에 넣는다

        await RenameBehindTheCacheAsync(id, "ChangedInDb");

        Assert.Equal("Shared", await GetNameAsync(secondManager, id)); // 둘째 인스턴스는 L1 이 비어 있어 Redis 에서 읽는다
    }

    [Fact]
    public async Task ChangeOnOneInstance_EvictsTheOtherInstancesLocalCopy()
    {
        var (firstClient, _) = await TestUsers.SignInAsync(_first, "cache-backplane-1@example.com", administrator: true);
        var (secondClient, _) = await TestUsers.SignInAsync(_second, "cache-backplane-2@example.com", administrator: true);
        var id = await CreateAsync(firstClient, "Original");
        Assert.Equal("Original", await GetNameAsync(firstClient, id)); // 첫 인스턴스의 L1 에 들어간다

        await RenameBehindTheCacheAsync(id, "ChangedInDb");
        var changed = await secondClient.PutAsJsonAsync($"/api/products/{id}/status", new { status = "OnSale" }, TestContext.Current.CancellationToken);
        changed.EnsureSuccessStatusCode();

        // 백플레인 메시지는 Redis pub/sub 로 비동기 전달된다 — 짧게 기다리며 확인한다(L1 만료 10분을 기다리지 않는다).
        var name = await EventuallyAsync(() => GetNameAsync(firstClient, id), expected: "ChangedInDb");
        Assert.Equal("ChangedInDb", name);
    }

    [Fact]
    public async Task BothInstances_SubscribeToTheBackplane()
    {
        // 무효화가 닿는 경로가 백플레인인지 확인한다 — 구독이 실패해도 앱은 오류를 기록하고 계속 돌기 때문에 응답만으로는 드러나지 않는다.
        await GetNameOrNullAsync(_first.CreateClient());
        await GetNameOrNullAsync(_second.CreateClient());
        await using var redis = await ConnectionMultiplexer.ConnectAsync(infrastructure.RedisConnectionString);

        long subscribers = 0;
        for (var attempt = 0; attempt < 50 && subscribers < 2; attempt++)
        {
            var server = redis.GetServers()[0];
            var channels = await server.SubscriptionChannelsAsync(RedisChannel.Pattern("FusionCache.Backplane*"));
            subscribers = 0;
            foreach (var channel in channels)
            {
                subscribers += await server.SubscriptionSubscriberCountAsync(channel);
            }

            await Task.Delay(100, TestContext.Current.CancellationToken);
        }

        Assert.True(subscribers >= 2, $"백플레인 구독자 {subscribers} — 두 인스턴스가 모두 구독해야 한다");
    }

    private static async Task GetNameOrNullAsync(HttpClient client) =>
        await client.GetAsync("/api/products", TestContext.Current.CancellationToken);

    private static async Task<string?> EventuallyAsync(Func<Task<string?>> read, string expected)
    {
        string? value = null;
        for (var attempt = 0; attempt < 50; attempt++)
        {
            value = await read();
            if (value == expected)
            {
                return value;
            }

            await Task.Delay(100, TestContext.Current.CancellationToken);
        }

        return value;
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
