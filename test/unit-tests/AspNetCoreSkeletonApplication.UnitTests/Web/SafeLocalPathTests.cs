using AspNetCoreSkeletonApplication.Common.Web;

namespace AspNetCoreSkeletonApplication.UnitTests.Web;

/// <summary>소셜 로그인 뒤 돌아갈 주소 — 같은 출처 경로만. 브라우저가 지우는 탭 · 줄바꿈으로 다른 사이트(<c>//…</c>)가 되지 않게 한다.</summary>
public class SafeLocalPathTests
{
    [Theory]
    [InlineData("/products?page=2", "/products?page=2")]
    [InlineData(null, "/")]
    [InlineData("", "/")]
    [InlineData("https://evil.example", "/")]
    [InlineData("//evil.example", "/")]
    [InlineData("/\\evil.example", "/")]
    [InlineData("/\t/evil.example", "/")]
    [InlineData("/\n/evil.example", "/")]
    [InlineData("/\r\n/evil.example", "/")]
    [InlineData("/ /evil.example", "/")]
    public void OnlySameOriginPathsPass(string? value, string expected) =>
        Assert.Equal(expected, ExternalLoginEndpoints.SafeLocalPath(value));
}
