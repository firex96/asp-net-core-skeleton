using System.Net;
using System.Net.Http.Json;
using AspNetCoreSkeletonApplication.FunctionalTests.Support;

namespace AspNetCoreSkeletonApplication.FunctionalTests;

/// <summary>
/// 인가 규칙 — 기본은 로그인 필요, 조회는 익명으로 열었고, 상태 변경 · 삭제는 관리자 정책. cross-site 변경 요청은 로그인했어도 403.
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
    public async Task User_CanCreateButNotChangeStatusOrDelete()
    {
        var client = ClientAs("7");
        var created = await client.PostAsJsonAsync("/api/products", new { name = "Mine", price = 1 }, TestContext.Current.CancellationToken);
        var id = (await created.Content.ReadFromJsonAsync<IdOnly>(TestContext.Current.CancellationToken))!.Id;

        var status = await client.PutAsJsonAsync($"/api/products/{id}/status", new { status = "OnSale" }, TestContext.Current.CancellationToken);
        var delete = await client.DeleteAsync($"/api/products/{id}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, status.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
    }

    [Fact]
    public async Task Administrator_CanChangeStatus()
    {
        var client = ClientAs("1;Administrator");
        var created = await client.PostAsJsonAsync("/api/products", new { name = "Admin", price = 1 }, TestContext.Current.CancellationToken);
        var id = (await created.Content.ReadFromJsonAsync<IdOnly>(TestContext.Current.CancellationToken))!.Id;

        var status = await client.PutAsJsonAsync($"/api/products/{id}/status", new { status = "OnSale" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, status.StatusCode);
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

    private sealed record IdOnly(long Id);
}
