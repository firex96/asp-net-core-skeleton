using System.Net;
using System.Text;
using System.Text.Json;
using AspNetCoreSkeletonApplication.FunctionalTests.Support;

namespace AspNetCoreSkeletonApplication.FunctionalTests;

/// <summary>
/// 프레임워크가 만든 오류 응답도 HTTP 상태와 코드가 일치하고, 문구는 화면에 그대로 보여도 되는 문장이다
/// (.NET 타입 이름 · JSON 경로 · 영어 기본 문구 · 예외 메시지를 싣지 않는다).
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

    [Fact]
    public async Task JsonTypeMismatch_IsUserSentence()
    {
        var response = await PostTypedAsync("""{"price":"abc","name":"a","count":1}""");

        var problem = await AssertProblemAsync(response, HttpStatusCode.BadRequest, "NOT_VALID_ERROR", "G011");
        var messages = AllErrorMessages(problem);
        Assert.Contains("값의 형식이 올바르지 않아요.", messages);
        AssertNoTechnicalText(problem);
    }

    [Fact]
    public async Task JsonRequiredMissing_IsUserSentence()
    {
        var response = await PostTypedAsync("""{"name":"a","count":1}""");

        var problem = await AssertProblemAsync(response, HttpStatusCode.BadRequest, "NOT_VALID_ERROR", "G011");
        Assert.Contains("값의 형식이 올바르지 않아요.", AllErrorMessages(problem));
        AssertNoTechnicalText(problem);
    }

    [Fact]
    public async Task DataAnnotations_UseKoreanDefaultMessages()
    {
        var required = await AssertProblemAsync(await PostTypedAsync("""{"price":1,"count":1}"""), HttpStatusCode.BadRequest, "NOT_VALID_ERROR", "G011");
        Assert.Equal(["값을 넣어 주세요."], ErrorsOf(required, "Name"));

        var tooLong = await AssertProblemAsync(await PostTypedAsync("""{"price":1,"name":"abcdef","count":1}"""), HttpStatusCode.BadRequest, "NOT_VALID_ERROR", "G011");
        Assert.Equal(["5자 이하로 넣어 주세요."], ErrorsOf(tooLong, "Name"));

        var outOfRange = await AssertProblemAsync(await PostTypedAsync("""{"price":1,"name":"a","count":11}"""), HttpStatusCode.BadRequest, "NOT_VALID_ERROR", "G011");
        Assert.Equal(["1 이상 10 이하로 넣어 주세요."], ErrorsOf(outOfRange, "Count"));

        AssertNoTechnicalText(required);
        AssertNoTechnicalText(tooLong);
        AssertNoTechnicalText(outOfRange);
    }

    [Fact]
    public async Task InvalidBody_HasNoBodyParameterNameError()
    {
        var problem = await AssertProblemAsync(await PostTypedAsync("""{"price":"abc"}"""), HttpStatusCode.BadRequest, "NOT_VALID_ERROR", "G011");

        Assert.False(problem.GetProperty("errors").TryGetProperty("model", out _));
    }

    private Task<HttpResponseMessage> PostTypedAsync(string json) =>
        factory.CreateAuthenticatedClient().PostAsync(
            "/error-probe/typed", new StringContent(json, Encoding.UTF8, "application/json"), TestContext.Current.CancellationToken);

    private static async Task<JsonElement> AssertProblemAsync(HttpResponseMessage response, HttpStatusCode status, string code, string divisionCode)
    {
        Assert.Equal(status, response.StatusCode);
        var problem = await ProblemResponse.ReadAsync(response);
        Assert.Equal((int)status, problem.GetProperty("status").GetInt32());
        Assert.Equal(code, problem.GetProperty("code").GetString());
        Assert.Equal(divisionCode, problem.GetProperty("divisionCode").GetString());
        return problem;
    }

    private static string[] ErrorsOf(JsonElement problem, string key) =>
        problem.GetProperty("errors").GetProperty(key).EnumerateArray().Select(e => e.GetString()!).ToArray();

    private static string[] AllErrorMessages(JsonElement problem) =>
        problem.GetProperty("errors").EnumerateObject().SelectMany(p => p.Value.EnumerateArray().Select(e => e.GetString()!)).ToArray();

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
