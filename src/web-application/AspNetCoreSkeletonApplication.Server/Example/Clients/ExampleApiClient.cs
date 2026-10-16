using AspNetCoreSkeletonApplication.Common.Web;
using AspNetCoreSkeletonApplication.Server.Example.Dtos;

namespace AspNetCoreSkeletonApplication.Server.Example.Clients;

/// <summary>
/// 외부 API 클라이언트 샘플. BizService 만 주입받는다. 등록은 Program.cs 의
/// <c>AddApiClient&lt;ExampleApiClient, ExampleApiOptions&gt;("Clients:ExampleApi")</c> 한 줄이다.
/// </summary>
public class ExampleApiClient(HttpClient httpClient)
{
    public async Task<ExampleStatusDto> GetStatusAsync(CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync("status", cancellationToken);
        return await response.ReadRequiredFromJsonAsync<ExampleStatusDto>(nameof(ExampleApiClient), cancellationToken);
    }
}
