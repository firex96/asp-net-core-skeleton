using AspNetCoreSkeletonApplication.Common.Errors;
using AspNetCoreSkeletonApplication.Common.Web;
using Microsoft.AspNetCore.Http;

namespace AspNetCoreSkeletonApplication.UnitTests.Web;

/// <summary>문장이 없는 4xx 에 싣는 상태별 문장 — 코드가 있는 4xx 상태마다 자기 문장이 있고, 401 · 403 은 SPA 의 대비 문장과 같다.</summary>
public class ErrorMessagesTests
{
    [Fact]
    public void ForStatus_EveryMappedClientErrorStatus_HasItsOwnSentence()
    {
        var fallback = ErrorMessages.ForStatus(StatusCodes.Status400BadRequest);
        var mapped = Enumerable.Range(401, 99).Where(status => ErrorCode.FromStatus(status).Status == status).ToList();

        Assert.NotEmpty(mapped);
        Assert.All(mapped, status => Assert.NotEqual(fallback, ErrorMessages.ForStatus(status)));
    }

    // SPA(angular · nuxt · sveltekit)의 ApiError 는 detail 이 없을 때 이 문장을 보인다 — 서버 문장이 같아야 화면 문구가 그대로다
    [Theory]
    [InlineData(StatusCodes.Status401Unauthorized, "로그인이 필요해요.")]
    [InlineData(StatusCodes.Status403Forbidden, "이 작업을 할 권한이 없어요.")]
    public void ForStatus_AuthenticationStatuses_MatchSpaFallbackSentences(int status, string sentence) =>
        Assert.Equal(sentence, ErrorMessages.ForStatus(status));
}
