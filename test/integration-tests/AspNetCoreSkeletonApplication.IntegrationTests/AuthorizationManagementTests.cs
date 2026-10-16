using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AspNetCoreSkeletonApplication.IntegrationTests.Support;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AspNetCoreSkeletonApplication.IntegrationTests;

/// <summary>
/// 그룹 · 역할 · 권한을 관리 API 로 바꾸고, 바뀐 권한이 곧바로 판정에 반영되는지(캐시 무효화) 실제 DB · Redis 로 본다.
/// 마지막 관리자를 없애는 변경은 409 로 되돌려진다.
/// </summary>
[Collection(InfrastructureCollection.Name)]
public class AuthorizationManagementTests(InfrastructureFixture infrastructure) : IAsyncLifetime
{
    private const string Database = "authorization_management";

    private WebApplicationFactory<Program> _factory = null!;

    public async ValueTask InitializeAsync()
    {
        await infrastructure.RecreateDatabaseAsync(Database);
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => infrastructure.ConfigureApp(builder, Database));
    }

    public async ValueTask DisposeAsync() => await _factory.DisposeAsync();

    [Fact]
    public async Task GrantingAndRevokingThroughAGroup_TakesEffectOnTheNextRequest()
    {
        var (admin, _) = await TestUsers.SignInAsync(_factory, "grant-admin@example.com", administrator: true);
        var (user, userId) = await TestUsers.SignInAsync(_factory, "grant-user@example.com", administrator: false);
        var productId = await CreateProductAsync(user);
        Assert.Equal(HttpStatusCode.Forbidden, (await ChangeStatusAsync(user, productId, "OnSale")).StatusCode);

        var roleId = await CreateAsync(admin, "/api/authorization/roles",
            new { name = "Product managers", description = "", permissions = new[] { "products.manage" } });
        var groupId = await CreateAsync(admin, "/api/authorization/groups", new { name = "Editors", description = "상품 편집" });
        await SucceedAsync(admin.PutAsync($"/api/authorization/groups/{groupId}/roles/{roleId}", null, Token));
        await SucceedAsync(admin.PutAsync($"/api/authorization/groups/{groupId}/members/{userId}", null, Token));

        Assert.Equal(HttpStatusCode.OK, (await ChangeStatusAsync(user, productId, "OnSale")).StatusCode);
        Assert.Contains("products.manage", await PermissionsOfAsync(user));

        await SucceedAsync(admin.DeleteAsync($"/api/authorization/groups/{groupId}/members/{userId}", Token));

        Assert.Equal(HttpStatusCode.Forbidden, (await ChangeStatusAsync(user, productId, "Discontinued")).StatusCode);
        Assert.DoesNotContain("products.manage", await PermissionsOfAsync(user));
    }

    [Fact]
    public async Task GroupDetails_ListMembersAndRolesWithPermissions()
    {
        var (admin, adminId) = await TestUsers.SignInAsync(_factory, "details-admin@example.com", administrator: true);
        var groups = await admin.GetFromJsonAsync<JsonElement>("/api/authorization/groups", Token);
        var administrators = groups.EnumerateArray().Single(group => group.GetProperty("name").GetString() == TestUsers.AdministratorsGroup);
        var groupId = administrators.GetProperty("id").GetInt64();

        var members = await admin.GetFromJsonAsync<JsonElement>($"/api/authorization/groups/{groupId}/members", Token);
        var roles = await admin.GetFromJsonAsync<JsonElement>($"/api/authorization/groups/{groupId}/roles", Token);

        Assert.Contains(members.EnumerateArray(), member =>
            member.GetProperty("userId").GetInt64() == adminId && member.GetProperty("email").GetString() == "details-admin@example.com");
        var role = Assert.Single(roles.EnumerateArray());
        Assert.Equal(["authorization.manage", "products.manage"], role.GetProperty("permissions").EnumerateArray().Select(code => code.GetString()));
    }

    [Fact]
    public async Task RemovingTheLastAuthorizationManager_IsRejected()
    {
        var (admin, adminId) = await TestUsers.SignInAsync(_factory, "last-admin@example.com", administrator: true);
        var groups = await admin.GetFromJsonAsync<JsonElement>("/api/authorization/groups", Token);
        var groupId = groups.EnumerateArray().Single(group => group.GetProperty("name").GetString() == TestUsers.AdministratorsGroup)
            .GetProperty("id").GetInt64();
        var roles = await admin.GetFromJsonAsync<JsonElement>("/api/authorization/roles", Token);
        var roleId = roles.EnumerateArray().Single(role => role.GetProperty("name").GetString() == "Administrator").GetProperty("id").GetInt64();

        var removeMember = await admin.DeleteAsync($"/api/authorization/groups/{groupId}/members/{adminId}", Token);
        var deleteGroup = await admin.DeleteAsync($"/api/authorization/groups/{groupId}", Token);
        var narrowRole = await admin.PutAsJsonAsync($"/api/authorization/roles/{roleId}",
            new { name = "Administrator", description = "", permissions = new[] { "products.manage" } }, Token);

        Assert.Equal(HttpStatusCode.Conflict, removeMember.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, deleteGroup.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, narrowRole.StatusCode);
        Assert.Contains("authorization.manage", await PermissionsOfAsync(admin)); // 되돌려졌다
    }

    [Fact]
    public async Task UnknownPermissionCode_Is400()
    {
        var (admin, _) = await TestUsers.SignInAsync(_factory, "unknown-code@example.com", administrator: true);

        var response = await admin.PostAsJsonAsync("/api/authorization/roles",
            new { name = "Bogus", description = "", permissions = new[] { "products.fly" } }, Token);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DuplicateGroupName_Is409()
    {
        var (admin, _) = await TestUsers.SignInAsync(_factory, "duplicate@example.com", administrator: true);

        var response = await admin.PostAsJsonAsync("/api/authorization/groups", new { name = TestUsers.AdministratorsGroup, description = "" }, Token);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static async Task<long> CreateProductAsync(HttpClient client) =>
        await CreateAsync(client, "/api/products", new { name = "Lamp", price = 100 });

    private static Task<HttpResponseMessage> ChangeStatusAsync(HttpClient client, long id, string status) =>
        client.PutAsJsonAsync($"/api/products/{id}/status", new { status }, Token);

    private static async Task<long> CreateAsync(HttpClient client, string path, object body)
    {
        var response = await client.PostAsJsonAsync(path, body, Token);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync(Token)).RootElement.GetProperty("id").GetInt64();
    }

    private static async Task SucceedAsync(Task<HttpResponseMessage> request) =>
        Assert.Equal(HttpStatusCode.NoContent, (await request).StatusCode);

    private static async Task<string?[]> PermissionsOfAsync(HttpClient client) =>
        (await client.GetFromJsonAsync<JsonElement>("/api/auth/me", Token)).GetProperty("permissions").EnumerateArray()
            .Select(code => code.GetString()).ToArray();
}
