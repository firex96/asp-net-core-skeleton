using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AspNetCoreSkeletonApplication.Common.Web;

public static class ApiClientServiceCollectionExtensions
{
    /// <summary>
    /// 외부 API 클라이언트(typed HttpClient)를 등록한다. 클라이언트마다 이 한 줄이면 아래가 모두 붙는다.
    /// <list type="bullet">
    /// <item><paramref name="configurationSection"/> 의 <typeparamref name="TOptions"/> 바인딩과 기동 시 검증</item>
    /// <item>응답 없음(연결·타임아웃·회로 차단) → <c>ApiCallException</c></item>
    /// <item>표준 resilience — 재시도, 시도별·전체 타임아웃, 회로 차단기</item>
    /// <item>추적 ID 전파 — 시도마다 <c>X-Trace-Id</c></item>
    /// </list>
    /// </summary>
    public static IHttpClientBuilder AddApiClient<TClient, TOptions>(
        this IServiceCollection services, string configurationSection)
        where TClient : class
        where TOptions : ApiClientOptions
    {
        services.AddOptions<TOptions>()
            .BindConfiguration(configurationSection)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var clientName = typeof(TClient).Name;
        var builder = services.AddHttpClient<TClient>((serviceProvider, httpClient) =>
            httpClient.BaseAddress = serviceProvider.GetRequiredService<IOptions<TOptions>>().Value.BaseAddress);

        // 먼저 붙인 핸들러가 바깥이다 — 예외 변환 → resilience → 추적 ID(시도마다) 순서.
        builder.AddHttpMessageHandler(() => new ApiCallExceptionHandler(clientName));
        builder.AddStandardResilienceHandler();
        builder.AddHttpMessageHandler(() => new TraceIdPropagationHandler());

        return builder;
    }
}
