using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AspNetCoreSkeletonApplication.Common.Web;
using AspNetCoreSkeletonApplication.FunctionalTests.Support;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

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

        var problem = await AssertProblemAsync(response, HttpStatusCode.MethodNotAllowed, "METHOD_NOT_ALLOWED_ERROR", "G017", "이 주소에서 지원하지 않는 요청 방식이에요.");
        Assert.Contains("GET", response.Content.Headers.Allow);
        AssertNoTechnicalText(problem);
    }

    [Fact]
    public async Task WrongContentType_Is415()
    {
        var content = new StringContent("x", Encoding.UTF8, "text/plain");
        var response = await factory.CreateAuthenticatedClient().PostAsync("/error-probe/json-only", content, TestContext.Current.CancellationToken);

        await AssertProblemAsync(response, HttpStatusCode.UnsupportedMediaType, "UNSUPPORTED_MEDIA_TYPE_ERROR", "G020", "지원하지 않는 요청 본문 형식이에요.");
    }

    [Fact]
    public async Task BadHttpRequest_UsesItsStatusAndFixedDetail()
    {
        var response = await factory.CreateAuthenticatedClient().GetAsync("/error-probe/bad-http-request", TestContext.Current.CancellationToken);

        var problem = await AssertProblemAsync(response, HttpStatusCode.RequestEntityTooLarge, "PAYLOAD_TOO_LARGE_ERROR", "G019", "요청 본문이 너무 커요.");
        Assert.DoesNotContain("30000000", problem.GetRawText());
    }

    [Fact]
    public async Task UnmappedStatus_FallsBackToCodeAndItsStatus()
    {
        var response = await factory.CreateAuthenticatedClient().GetAsync("/error-probe/teapot", TestContext.Current.CancellationToken);

        await AssertProblemAsync(response, HttpStatusCode.BadRequest, "BAD_REQUEST_ERROR", "G001", "요청을 처리할 수 없어요. 보낸 값을 확인해 주세요.");
    }

    [Fact]
    public async Task ServiceUnavailableStatus_Is503Code()
    {
        var response = await factory.CreateAuthenticatedClient().GetAsync("/error-probe/unavailable", TestContext.Current.CancellationToken);

        await AssertProblemAsync(response, HttpStatusCode.ServiceUnavailable, "SERVICE_UNAVAILABLE_ERROR", "G016", detail: null);
    }

    [Fact]
    public async Task ForbidResult_Is403WithSentence()
    {
        var response = await factory.CreateAuthenticatedClient().GetAsync("/error-probe/forbidden", TestContext.Current.CancellationToken);

        await AssertProblemAsync(response, HttpStatusCode.Forbidden, "FORBIDDEN_ERROR", "G008", "이 작업을 할 권한이 없어요.");
    }

    [Fact]
    public async Task AnonymousUnknownApiPath_Is401WithSentence()
    {
        // /api 아래라 SPA 폴백이 받지 않는다 — fallback 인가 정책이 라우팅 판정보다 먼저 401 을 낸다
        var response = await factory.CreateClient().GetAsync("/api/error-probe/no-such-path", TestContext.Current.CancellationToken);

        await AssertProblemAsync(response, HttpStatusCode.Unauthorized, "UNAUTHORIZED_ERROR", "G015", "로그인이 필요해요.");
    }

    // MVC Problem() 은 만들 때 customize 를 거친다. 최소 API TypedResults.Problem 은 IProblemDetailsService 가 쓰지 못하면
    // (Accept 가 JSON 을 받지 않으면) 원래 객체를 그대로 JSON 으로 쓴다 — 두 경로 모두, 두 Accept 모두 확인한다
    public static TheoryData<string, int, string, string, string> ServerProblems
    {
        get
        {
            var data = new TheoryData<string, int, string, string, string>();
            foreach (var probe in new[] { "server-problem", "minimal-problem" })
            {
                foreach (var accept in new[] { "application/json", "text/html" })
                {
                    data.Add(probe, 500, "INTERNAL_SERVER_ERROR", "G999", accept);
                    data.Add(probe, 502, "EXTERNAL_API_ERROR", "G014", accept);
                    data.Add(probe, 503, "SERVICE_UNAVAILABLE_ERROR", "G016", accept);
                }
            }

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(ServerProblems))]
    public async Task ServerProblemWithDetail_HasNoDetail(string probe, int status, string code, string divisionCode, string accept)
    {
        var response = await GetAsync($"/error-probe/{probe}/{status}", accept);

        var problem = await AssertProblemAsync(response, (HttpStatusCode)status, code, divisionCode, detail: null);
        Assert.DoesNotContain(ErrorProbeController.InternalDetail, problem.GetRawText());
    }

    [Fact]
    public async Task StatusCodePage_NotAcceptingJson_IsStillProblemDetails()
    {
        var response = await GetAsync("/api/error-probe/no-such-path", "text/html");

        await AssertProblemAsync(response, HttpStatusCode.NotFound, "NOT_FOUND_ERROR", "G009", "요청한 대상을 찾을 수 없어요.");
    }

    [Fact]
    public void ProblemDetailsService_IsSkeletonDispatcher_AfterAddControllers() =>
        Assert.IsType<ProblemDetailsDispatcher>(factory.Services.GetRequiredService<IProblemDetailsService>());

    [Fact]
    public async Task JsonTypeMismatch_IsUserSentence()
    {
        var response = await PostTypedAsync("""{"price":"abc","name":"a","count":1}""");

        var problem = await AssertValidationProblemAsync(response);
        var messages = AllErrorMessages(problem);
        Assert.Contains("값의 형식이 올바르지 않아요.", messages);
        AssertNoTechnicalText(problem);
    }

    [Fact]
    public async Task JsonRequiredMissing_IsUserSentence()
    {
        var response = await PostTypedAsync("""{"name":"a","count":1}""");

        var problem = await AssertValidationProblemAsync(response);
        Assert.Contains("값의 형식이 올바르지 않아요.", AllErrorMessages(problem));
        AssertNoTechnicalText(problem);
    }

    [Fact]
    public async Task DataAnnotations_UseKoreanDefaultMessages()
    {
        var required = await AssertValidationProblemAsync(await PostTypedAsync("""{"price":1,"count":1}"""));
        Assert.Equal(["값을 넣어 주세요."], ErrorsOf(required, "Name"));

        var tooLong = await AssertValidationProblemAsync(await PostTypedAsync("""{"price":1,"name":"abcdef","count":1}"""));
        Assert.Equal(["5자 이하로 넣어 주세요."], ErrorsOf(tooLong, "Name"));

        var outOfRange = await AssertValidationProblemAsync(await PostTypedAsync("""{"price":1,"name":"a","count":11}"""));
        Assert.Equal(["1 이상 10 이하로 넣어 주세요."], ErrorsOf(outOfRange, "Count"));

        AssertNoTechnicalText(required);
        AssertNoTechnicalText(tooLong);
        AssertNoTechnicalText(outOfRange);
    }

    [Fact]
    public async Task InvalidBody_HasNoBodyParameterNameError()
    {
        var problem = await AssertValidationProblemAsync(await PostTypedAsync("""{"price":"abc"}"""));

        Assert.False(problem.GetProperty("errors").TryGetProperty("model", out _));
    }

    private async Task<HttpResponseMessage> GetAsync(string path, string accept)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(accept));
        return await factory.CreateAuthenticatedClient().SendAsync(request, TestContext.Current.CancellationToken);
    }

    private Task<HttpResponseMessage> PostTypedAsync(string json) =>
        factory.CreateAuthenticatedClient().PostAsync(
            "/error-probe/typed", new StringContent(json, Encoding.UTF8, "application/json"), TestContext.Current.CancellationToken);

    /// <summary>검증 실패 — 문장은 errors 에 있고 detail 은 없다.</summary>
    private static Task<JsonElement> AssertValidationProblemAsync(HttpResponseMessage response) =>
        AssertProblemAsync(response, HttpStatusCode.BadRequest, "NOT_VALID_ERROR", "G011", detail: null);

    /// <summary>상태 · 코드와 detail 을 함께 확인한다. <paramref name="detail"/> 이 null 이면 detail 이 없어야 한다.</summary>
    private static async Task<JsonElement> AssertProblemAsync(
        HttpResponseMessage response, HttpStatusCode status, string code, string divisionCode, string? detail)
    {
        Assert.Equal(status, response.StatusCode);
        var problem = await ProblemResponse.ReadAsync(response);
        Assert.Equal((int)status, problem.GetProperty("status").GetInt32());
        Assert.Equal(code, problem.GetProperty("code").GetString());
        Assert.Equal(divisionCode, problem.GetProperty("divisionCode").GetString());
        if (detail is null)
        {
            Assert.False(problem.TryGetProperty("detail", out _));
        }
        else
        {
            Assert.Equal(detail, problem.GetProperty("detail").GetString());
        }
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
