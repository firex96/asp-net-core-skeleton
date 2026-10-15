namespace AspNetCoreSkeletonApplication.Common.Web;

/// <summary>인가 정책 이름. 컨트롤러는 역할 이름이 아니라 이 정책으로 인가한다(<c>[Authorize(Policy = Policies.Administrator)]</c>).</summary>
public static class Policies
{
    public const string Administrator = "Administrator";
}
