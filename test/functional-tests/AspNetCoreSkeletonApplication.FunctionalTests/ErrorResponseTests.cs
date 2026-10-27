using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AspNetCoreSkeletonApplication.FunctionalTests.Support;

namespace AspNetCoreSkeletonApplication.FunctionalTests;

/// <summary>모든 에러 응답이 ProblemDetails 이고 code · divisionCode · traceId 를 가진다.</summary>
public class ErrorResponseTests(ErrorProbeWebApplicationFactory factory) : IClassFixture<ErrorProbeWebApplicationFactory>
{
    [Theory]
    [InlineData("not-found", HttpStatusCode.NotFound, "NOT_FOUND_ERROR", "G009", "상품 42 가 없다")]
    [InlineData("bad-request", HttpStatusCode.BadRequest, "BAD_REQUEST_ERROR", "G001", "수량은 1 이상이다")]
    [InlineData("conflict", HttpStatusCode.Conflict, "CONFLICT_ERROR", "G013", "판매 종료된 상품은 다시 팔 수 없다")]
    public async Task CodedException_BecomesProblemDetailsWithItsCode(
        string probe, HttpStatusCode status, string code, string divisionCode, string detail)
    {
        var response = await factory.CreateClient().GetAsync($"/error-probe/{probe}", TestContext.Current.CancellationToken);

        Assert.Equal(status, response.StatusCode);
        var problem = await ProblemResponse.ReadAsync(response);
        Assert.Equal(code, problem.GetProperty("code").GetString());
        Assert.Equal(divisionCode, problem.GetProperty("divisionCode").GetString());
        Assert.Equal(detail, problem.GetProperty("detail").GetString());
        Assert.False(string.IsNullOrEmpty(problem.GetProperty("traceId").GetString()));
    }

    [Fact]
    public async Task UnhandledException_Is500WithoutInternalMessage()
    {
        var response = await factory.CreateClient().GetAsync("/error-probe/unhandled", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.DoesNotContain("연결 문자열", body);
        var problem = JsonDocument.Parse(body).RootElement;
        Assert.Equal("INTERNAL_SERVER_ERROR", problem.GetProperty("code").GetString());
        Assert.Equal("G999", problem.GetProperty("divisionCode").GetString());
    }

    [Fact]
    public async Task ValidationFailure_IsNotValidErrorWithFieldErrors()
    {
        var response = await factory.CreateClient().PostAsJsonAsync(
            "/error-probe/validate", new { name = (string?)null }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await ProblemResponse.ReadAsync(response);
        Assert.Equal("NOT_VALID_ERROR", problem.GetProperty("code").GetString());
        Assert.Equal("G011", problem.GetProperty("divisionCode").GetString());
        Assert.True(problem.GetProperty("errors").TryGetProperty("Name", out _));
        Assert.False(problem.TryGetProperty("detail", out _));
    }

    [Fact]
    public async Task StatusCodeResult_GetsCodeFromStatus()
    {
        var response = await factory.CreateClient().GetAsync("/error-probe/status-not-found", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await ProblemResponse.ReadAsync(response);
        Assert.Equal("NOT_FOUND_ERROR", problem.GetProperty("code").GetString());
        Assert.Equal("요청한 대상을 찾을 수 없어요.", problem.GetProperty("detail").GetString());
    }
}
