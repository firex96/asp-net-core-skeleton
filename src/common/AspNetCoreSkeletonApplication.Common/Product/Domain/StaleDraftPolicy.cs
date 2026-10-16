namespace AspNetCoreSkeletonApplication.Common.Product.Domain;

/// <summary>
/// "오래 손대지 않은 초안" 의 기준. 마지막 수정이 기준 시각보다 이전인 Draft 상품은 판매 종료 대상이다.
/// 순수 함수 — 현재 시각과 기간은 호출하는 쪽이 넘긴다.
/// </summary>
public static class StaleDraftPolicy
{
    public static DateTimeOffset CutoffOf(DateTimeOffset now, int staleAfterDays) =>
        staleAfterDays >= 1
            ? now.AddDays(-staleAfterDays)
            : throw new ArgumentOutOfRangeException(nameof(staleAfterDays), staleAfterDays, "기간은 1일 이상이다");
}
