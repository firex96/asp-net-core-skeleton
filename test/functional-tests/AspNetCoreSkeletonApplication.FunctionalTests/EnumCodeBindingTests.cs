using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AspNetCoreSkeletonApplication.FunctionalTests.Support;

namespace AspNetCoreSkeletonApplication.FunctionalTests;

/// <summary>열거형은 C# 이름이 아니라 외부 코드로 주고받는다 — 쿼리 바인딩, JSON 본문, JSON 응답 모두.</summary>
public class EnumCodeBindingTests(ErrorProbeWebApplicationFactory factory) : IClassFixture<ErrorProbeWebApplicationFactory>
{
    [Fact]
    public async Task Query_BindsCodeAndResponseWritesCode()
    {
        var body = await factory.CreateClient().GetStringAsync("/binding-probe/color?color=dark-red", TestContext.Current.CancellationToken);

        Assert.Equal("dark-red", JsonDocument.Parse(body).RootElement.GetProperty("color").GetString());
    }

    [Fact]
    public async Task Query_CSharpName_Is400NotValid()
    {
        var response = await factory.CreateClient().GetAsync("/binding-probe/color?color=DarkRed", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).RootElement;
        Assert.Equal("NOT_VALID_ERROR", problem.GetProperty("code").GetString());
    }

    [Fact]
    public async Task JsonBody_ReadsCode()
    {
        var response = await factory.CreateClient().PostAsJsonAsync(
            "/binding-probe/color", new { color = "light-blue" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).RootElement;
        Assert.Equal("light-blue", body.GetProperty("color").GetString());
    }
}
