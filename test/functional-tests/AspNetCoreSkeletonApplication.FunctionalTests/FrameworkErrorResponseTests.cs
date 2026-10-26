using System.Net;
using System.Text;
using System.Text.Json;
using AspNetCoreSkeletonApplication.FunctionalTests.Support;

namespace AspNetCoreSkeletonApplication.FunctionalTests;

/// <summary>
/// 프레임워크가 만든 오류 응답도 HTTP 상태와 코드가 일치하고, 문구에 예외 메시지를 싣지 않는다.
/// </summary>
public class FrameworkErrorResponseTests(ErrorProbeWebApplicationFactory factory) : IClassFixture<ErrorProbeWebApplicationFactory>
{
    private static readonly string[] TechnicalFragments =
        ["System.", "could not be converted", "Path: $", "LineNumber", "missing required properties", "TypedProbeModel", "The ", "Exception"];

    [Fact]
    public async Task WrongMethod_Is405WithAllow()
    {
        var response = await factory.CreateAuthenticatedClient().DeleteAsync("/api/error-probe/get-only", TestContext.Current.CancellationToken);

        var problem = await AssertProblemAsync(response, HttpStatusCode.MethodNotAllowed, "METHOD_NOT_ALLOWED_ERROR", "G017");
        Assert.Contains("GET", response.Content.Headers.Allow);
        AssertNoTechnicalText(problem);
    }

    [Fact]
    public async Task WrongContentType_Is415()
    {
        var content = new StringContent("x", Encoding.UTF8, "text/plain");
        var response = await factory.CreateAuthenticatedClient().PostAsync("/error-probe/json-only", content, TestContext.Current.CancellationToken);

        await AssertProblemAsync(response, HttpStatusCode.UnsupportedMediaType, "UNSUPPORTED_MEDIA_TYPE_ERROR", "G020");
    }

    [Fact]
    public async Task BadHttpRequest_UsesItsStatusAndFixedDetail()
    {
        var response = await factory.CreateAuthenticatedClient().GetAsync("/error-probe/bad-http-request", TestContext.Current.CancellationToken);

        var problem = await AssertProblemAsync(response, HttpStatusCode.RequestEntityTooLarge, "PAYLOAD_TOO_LARGE_ERROR", "G019");
        Assert.Equal("요청 본문이 너무 커요.", problem.GetProperty("detail").GetString());
        Assert.DoesNotContain("30000000", problem.GetRawText());
    }

    [Fact]
    public async Task UnmappedStatus_FallsBackToCodeAndItsStatus()
    {
        var response = await factory.CreateAuthenticatedClient().GetAsync("/error-probe/teapot", TestContext.Current.CancellationToken);

        await AssertProblemAsync(response, HttpStatusCode.BadRequest, "BAD_REQUEST_ERROR", "G001");
    }

    [Fact]
    public async Task ServiceUnavailableStatus_Is503Code()
    {
        var response = await factory.CreateAuthenticatedClient().GetAsync("/error-probe/unavailable", TestContext.Current.CancellationToken);

        await AssertProblemAsync(response, HttpStatusCode.ServiceUnavailable, "SERVICE_UNAVAILABLE_ERROR", "G016");
    }

    private static async Task<JsonElement> AssertProblemAsync(HttpResponseMessage response, HttpStatusCode status, string code, string divisionCode)
    {
        Assert.Equal(status, response.StatusCode);
        var problem = await ProblemResponse.ReadAsync(response);
        Assert.Equal((int)status, problem.GetProperty("status").GetInt32());
        Assert.Equal(code, problem.GetProperty("code").GetString());
        Assert.Equal(divisionCode, problem.GetProperty("divisionCode").GetString());
        return problem;
    }

    private static void AssertNoTechnicalText(JsonElement problem)
    {
        var raw = problem.TryGetProperty("errors", out var errors) ? errors.GetRawText() : "";
        raw += problem.TryGetProperty("detail", out var detail) ? detail.GetRawText() : "";
        foreach (var fragment in TechnicalFragments)
        {
            Assert.DoesNotContain(fragment, raw);
        }
    }
}
