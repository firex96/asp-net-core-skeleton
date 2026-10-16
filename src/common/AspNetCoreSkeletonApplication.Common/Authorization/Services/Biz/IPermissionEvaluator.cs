namespace AspNetCoreSkeletonApplication.Common.Authorization.Services.Biz;

/// <summary>
/// 권한 판정의 유일한 입구. 정책 핸들러(<c>[HasPermission]</c>)와 리소스 핸들러는 이것만 부른다 — 판정 방식을 바꿀 때(예: ReBAC)
/// 이 구현 하나만 바꾼다. 쿠키나 토큰에 굳은 클레임을 쓰지 않고 요청마다 서버가 판정한다(캐시 · 백플레인으로 즉시 반영).
/// </summary>
public interface IPermissionEvaluator
{
    Task<IReadOnlySet<string>> ListPermissionsAsync(long userId, CancellationToken cancellationToken);

    Task<bool> HasPermissionAsync(long userId, string permission, CancellationToken cancellationToken);
}
