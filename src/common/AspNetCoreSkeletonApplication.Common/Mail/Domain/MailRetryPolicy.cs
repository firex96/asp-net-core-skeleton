namespace AspNetCoreSkeletonApplication.Common.Mail.Domain;

/// <summary>
/// 보내지 못한 메일을 언제 다시 보낼지. 30초부터 두 배씩 늘려 기다리고, <see cref="MaxAttempts"/> 번 실패하면 포기한다(약 1시간).
/// 순수 함수 — 현재 시각은 호출하는 쪽이 넘긴다.
/// </summary>
public static class MailRetryPolicy
{
    public const int MaxAttempts = 8;

    private static readonly TimeSpan FirstDelay = TimeSpan.FromSeconds(30);

    /// <summary><paramref name="attempts"/> 번째 시도가 실패한 뒤의 다음 시도 시각. 더 시도하지 않으면 null 이다.</summary>
    public static DateTimeOffset? NextAttemptAt(int attempts, DateTimeOffset now) =>
        attempts switch
        {
            < 1 => throw new ArgumentOutOfRangeException(nameof(attempts), attempts, "시도 횟수는 1 이상이다"),
            >= MaxAttempts => null,
            _ => now + FirstDelay * Math.Pow(2, attempts - 1),
        };
}
