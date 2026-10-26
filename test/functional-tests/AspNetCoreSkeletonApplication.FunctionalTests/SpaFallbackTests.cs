using System.Net;
using System.Text.Json;
using AspNetCoreSkeletonApplication.FunctionalTests.Support;

namespace AspNetCoreSkeletonApplication.FunctionalTests;

/// <summary>SPA 화면은 /api 밖 경로에만 응답한다 — /api 아래 없는 경로는 404 ProblemDetails 이고, 405 판정도 가리지 않는다.</summary>
public class SpaFallbackTests(SpaFallbackWebApplicationFactory factory) : IClassFixture<SpaFallbackWebApplicationFactory>
{
    [Fact]
    public async Task NonApiPath_ServesSpa()
    {
        var response = await factory.CreateAuthenticatedClient().GetAsync("/some/page", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
    }

    [Theory]
    [InlineData("/api/nothing")]
    [InlineData("/api")]
    public async Task UnknownApiPath_Is404Problem(string path)
    {
        var response = await factory.CreateAuthenticatedClient().GetAsync(path, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await ProblemResponse.ReadAsync(response);
        Assert.Equal("NOT_FOUND_ERROR", problem.GetProperty("code").GetString());
    }

    [Fact]
    public async Task UnknownApiPath_Anonymous_Is401()
    {
        var response = await factory.CreateClient().GetAsync("/api/nothing", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task WrongMethodOnApi_Stays405()
    {
        var response = await factory.CreateAuthenticatedClient().DeleteAsync("/api/error-probe/get-only", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
        var problem = await ProblemResponse.ReadAsync(response);
        Assert.Equal("G017", problem.GetProperty("divisionCode").GetString());
    }

    /// <summary>한계 — 폴백이 GET · HEAD 만 받아, 없는 /api 경로의 다른 메서드는 404 가 아니라 405 다. 상태와 코드는 일치한다.</summary>
    [Fact]
    public async Task OtherMethodOnUnknownApiPath_Is405()
    {
        var response = await factory.CreateAuthenticatedClient().PostAsync("/api/nothing", null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
        var problem = await ProblemResponse.ReadAsync(response);
        Assert.Equal("METHOD_NOT_ALLOWED_ERROR", problem.GetProperty("code").GetString());
    }
}
