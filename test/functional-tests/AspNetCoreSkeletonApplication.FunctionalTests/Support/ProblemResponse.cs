using System.Text.Json;

namespace AspNetCoreSkeletonApplication.FunctionalTests.Support;

/// <summary>에러 응답이 ProblemDetails(<c>application/problem+json</c>)인지 확인하고 본문을 읽는다.</summary>
public static class ProblemResponse
{
    public static async Task<JsonElement> ReadAsync(HttpResponseMessage response)
    {
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        return JsonDocument.Parse(body).RootElement;
    }
}
