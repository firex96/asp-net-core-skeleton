using System.Net.Http.Json;
using System.Text.Json;

namespace AspNetCoreSkeletonApplication.E2ETests.Support;

/// <summary>Mailpit 받은 편지함 — 앱이 보낸 메일의 링크를 꺼낸다.</summary>
public sealed class Mailbox(Uri mailpitApi) : IDisposable
{
    private readonly HttpClient _mailpit = new() { BaseAddress = mailpitApi };

    /// <summary><paramref name="email"/> 에게 온 최신 메일 본문의 https 링크. 메일이 올 때까지 잠시 기다린다.</summary>
    public async Task<Uri> WaitForLinkAsync(string email)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var query = Uri.EscapeDataString($"to:\"{email}\"");
        for (var attempt = 0; attempt < 50; attempt++)
        {
            var search = await _mailpit.GetFromJsonAsync<JsonElement>($"api/v1/search?query={query}", cancellationToken);
            if (search.GetProperty("messages").GetArrayLength() > 0)
            {
                var id = search.GetProperty("messages")[0].GetProperty("ID").GetString();
                var message = await _mailpit.GetFromJsonAsync<JsonElement>($"api/v1/message/{id}", cancellationToken);
                var line = message.GetProperty("Text").GetString()!.Split('\n')
                    .Select(part => part.Trim())
                    .Single(part => part.StartsWith("https://", StringComparison.Ordinal));
                return new Uri(line);
            }

            await Task.Delay(200, cancellationToken);
        }

        throw new TimeoutException($"{email} 에게 온 메일이 없다");
    }

    public void Dispose() => _mailpit.Dispose();
}
