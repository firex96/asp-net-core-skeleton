using System.Net;
using System.Net.Http.Json;
using AspNetCoreSkeletonApplication.FunctionalTests.Support;

namespace AspNetCoreSkeletonApplication.FunctionalTests;

/// <summary>
/// 인가 규칙 — 기본은 로그인 필요, 조회는 익명으로 열었고, 상태 변경은 권한 <c>products.manage</c>, 삭제는 리소스 판정
/// (권한이 있거나 자기 Draft 상품), 인가 관리 API 는 <c>authorization.manage</c>. cross-site 변경 요청은 로그인했어도 403.
/// </summary>
public class AuthorizationTests(ProductWebApplicationFactory factory) : IClassFixture<ProductWebApplicationFactory>
{
    [Fact]
    public async Task Anonymous_CanReadButNotCreate()
    {
        var client = factory.CreateClient();

        var list = await client.GetAsync("/api/products", TestContext.Current.CancellationToken);
        var create = await client.PostAsJsonAsync("/api/products", new { name = "Anon", price = 1 }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, create.StatusCode);
    }

    [Fact]
    public async Task User_CanCreateButNotChangeStatus()
    {
        var client = ClientAs("7");
        var id = await CreateAsync(client, "Mine");

        var status = await client.PutAsJsonAsync($"/api/products/{id}/status", new { status = "OnSale" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, status.StatusCode);
    }

    [Fact]
    public async Task Manager_CanChangeStatus()
    {
        var client = ClientAs("1;products.manage");
        var id = await CreateAsync(client, "Managed");

        var status = await client.PutAsJsonAsync($"/api/products/{id}/status", new { status = "OnSale" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, status.StatusCode);
    }

    [Fact]
    public async Task Creator_CanDeleteOwnDraft_ButNotSomeoneElses()
    {
        var mine = await CreateAsync(ClientAs("7"), "Mine");
        var theirs = await CreateAsync(ClientAs("8"), "Theirs");

        var deleteMine = await ClientAs("7").DeleteAsync($"/api/products/{mine}", TestContext.Current.CancellationToken);
        var deleteTheirs = await ClientAs("7").DeleteAsync($"/api/products/{theirs}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, deleteMine.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, deleteTheirs.StatusCode);
    }

    [Fact]
    public async Task Creator_CannotDeleteOwnProductOnceOnSale_ButManagerCan()
    {
        var id = await CreateAsync(ClientAs("7"), "Selling");
        await ClientAs("1;products.manage").PutAsJsonAsync($"/api/products/{id}/status", new { status = "OnSale" }, TestContext.Current.CancellationToken);

        var byCreator = await ClientAs("7").DeleteAsync($"/api/products/{id}", TestContext.Current.CancellationToken);
        var byManager = await ClientAs("1;products.manage").DeleteAsync($"/api/products/{id}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, byCreator.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, byManager.StatusCode);
    }

    [Theory]
    [InlineData("/api/authorization/groups")]
    [InlineData("/api/authorization/roles")]
    [InlineData("/api/authorization/permissions")]
    public async Task AuthorizationManagement_NeedsItsPermission(string path)
    {
        var anonymous = await factory.CreateClient().GetAsync(path, TestContext.Current.CancellationToken);
        var user = await ClientAs("7;products.manage").GetAsync(path, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, user.StatusCode);
    }

    [Fact]
    public async Task PermissionCatalog_ListsEveryCode()
    {
        var catalog = await ClientAs("1;authorization.manage")
            .GetFromJsonAsync<IReadOnlyList<CodeOnly>>("/api/authorization/permissions", TestContext.Current.CancellationToken);

        Assert.Equal(["products.manage", "authorization.manage"], catalog!.Select(permission => permission.Code));
    }

    [Theory]
    [InlineData("cross-site", HttpStatusCode.Forbidden)]
    [InlineData("same-origin", HttpStatusCode.Created)]
    public async Task FetchMetadata_BlocksCrossSiteWrites(string fetchSite, HttpStatusCode expected)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/products") { Content = JsonContent.Create(new { name = "Csrf", price = 1 }) };
        request.Headers.Add("Sec-Fetch-Site", fetchSite);

        var response = await ClientAs("7").SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(expected, response.StatusCode);
    }

    private HttpClient ClientAs(string user)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.HeaderName, user);
        return client;
    }

    private static async Task<long> CreateAsync(HttpClient client, string name)
    {
        var created = await client.PostAsJsonAsync("/api/products", new { name, price = 1 }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        return (await created.Content.ReadFromJsonAsync<IdOnly>(TestContext.Current.CancellationToken))!.Id;
    }

    private sealed record IdOnly(long Id);

    private sealed record CodeOnly(string Code);
}
