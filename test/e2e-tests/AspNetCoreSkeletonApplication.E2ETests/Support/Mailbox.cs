using System.Net.Http.Json;
using System.Text.Json;

namespace AspNetCoreSkeletonApplication.E2ETests.Support;

/// <summary>Mailpit 받은 편지함 — 앱이 보낸 메일의 링크를 꺼낸다.</summary>
public sealed class Mailbox(Uri mailpitApi) : IDisposable
{
    private readonly HttpClient _mailpit = new() { BaseAddress = mailpitApi };

    /// <summary>
    /// <paramref name="email"/> 에게 온 메일 중 <paramref name="path"/>(예: <c>/reset-password</c>)로 가는 최신 링크. 메일은 아웃박스를 거쳐 오므로
    /// 올 때까지 잠시 기다린다.
    /// </summary>
    public async Task<Uri> WaitForLinkAsync(string email, string path)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var query = Uri.EscapeDataString($"to:\"{email}\"");
        for (var attempt = 0; attempt < 50; attempt++)
        {
            var search = await _mailpit.GetFromJsonAsync<JsonElement>($"api/v1/search?query={query}", cancellationToken);
            foreach (var summary in search.GetProperty("messages").EnumerateArray())
            {
                var message = await _mailpit.GetFromJsonAsync<JsonElement>($"api/v1/message/{summary.GetProperty("ID").GetString()}", cancellationToken);
                var link = message.GetProperty("Text").GetString()!.Split('\n')
                    .Select(part => part.Trim())
                    .Where(part => part.StartsWith("https://", StringComparison.Ordinal))
                    .Select(part => new Uri(part))
                    .FirstOrDefault(uri => uri.AbsolutePath == path);
                if (link is not null)
                {
                    return link;
                }
            }

            await Task.Delay(200, cancellationToken);
        }

        throw new TimeoutException($"{email} 에게 {path} 링크가 든 메일이 오지 않았다");
    }

    public void Dispose() => _mailpit.Dispose();
}
