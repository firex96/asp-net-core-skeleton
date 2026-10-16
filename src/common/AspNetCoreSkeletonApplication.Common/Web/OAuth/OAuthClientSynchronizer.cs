using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace AspNetCoreSkeletonApplication.Common.Web.OAuth;

/// <summary>
/// 구성(<c>Authentication:OAuthServer:Clients</c>)의 클라이언트를 기동할 때 저장소에 맞춘다 — 없으면 만들고 있으면 구성대로 고친다.
/// 클라이언트의 정본은 구성이고, 관리 화면은 두지 않는다. 구성에서 빠진 클라이언트는 지우지 않고 권한을 모두 비워 사용을 멈춘다 —
/// 행과 이미 발급한 토큰 · 동의 기록은 남고(지우면 감사 · 추적이 끊긴다), 토큰 · 인가 엔드포인트를 더 쓰지 못한다(리프레시도 막힌다).
/// 구성에 다시 넣으면 다음 기동 때 다시 켜진다. 목록이 비어 있으면 이 배포는 OAuth 클라이언트를 관리하지 않는 것으로 보고 저장소를
/// 건드리지 않는다(DB 를 쓰지 않는 기능 테스트 · OAuth 서버를 쓰지 않는 배포가 기동할 때 DB 에 묻지 않게).
/// </summary>
internal sealed partial class OAuthClientSynchronizer(
    IServiceScopeFactory scopeFactory, IOptions<OAuthServerOptions> options, ILogger<OAuthClientSynchronizer> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (options.Value.Clients.Count == 0)
        {
            return;
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
        foreach (var client in options.Value.Clients)
        {
            var descriptor = DescriptorOf(client);
            if (await manager.FindByClientIdAsync(client.ClientId, cancellationToken) is { } existing)
            {
                await manager.UpdateAsync(existing, descriptor, cancellationToken);
            }
            else
            {
                await manager.CreateAsync(descriptor, cancellationToken);
            }

            LogSynchronized(client.ClientId);
        }

        await DisableUnconfiguredAsync(manager, cancellationToken);
    }

    private async Task DisableUnconfiguredAsync(IOpenIddictApplicationManager manager, CancellationToken cancellationToken)
    {
        var configured = options.Value.Clients.Select(client => client.ClientId).ToHashSet(StringComparer.Ordinal);
        List<object> unconfigured = [];
        await foreach (var application in manager.ListAsync(count: null, offset: null, cancellationToken))
        {
            if (!configured.Contains(await manager.GetClientIdAsync(application, cancellationToken) ?? string.Empty)
                && !(await manager.GetPermissionsAsync(application, cancellationToken)).IsEmpty)
            {
                unconfigured.Add(application);
            }
        }

        // 목록을 다 읽은 뒤에 고친다 — 읽는 중에 고치면 같은 연결에서 열린 결과와 부딪힌다.
        foreach (var application in unconfigured)
        {
            var descriptor = new OpenIddictApplicationDescriptor();
            await manager.PopulateAsync(descriptor, application, cancellationToken);
            descriptor.Permissions.Clear();
            await manager.UpdateAsync(application, descriptor, cancellationToken);
            LogDisabled(descriptor.ClientId!);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    internal static OpenIddictApplicationDescriptor DescriptorOf(OAuthClientOptions client)
    {
        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = client.ClientId,
            ClientSecret = string.IsNullOrWhiteSpace(client.ClientSecret) ? null : client.ClientSecret,
            ClientType = string.IsNullOrWhiteSpace(client.ClientSecret) ? ClientTypes.Public : ClientTypes.Confidential,
            DisplayName = client.DisplayName,
            ConsentType = client.ConsentType == OAuthConsentType.Implicit ? ConsentTypes.Implicit : ConsentTypes.Explicit,
        };

        foreach (var uri in client.RedirectUris)
        {
            descriptor.RedirectUris.Add(new Uri(uri));
        }

        foreach (var uri in client.PostLogoutRedirectUris)
        {
            descriptor.PostLogoutRedirectUris.Add(new Uri(uri));
        }

        descriptor.Permissions.Add(Permissions.Endpoints.Token);
        if (client.GrantTypes.Contains(GrantTypes.AuthorizationCode))
        {
            descriptor.Permissions.Add(Permissions.Endpoints.Authorization);
            descriptor.Permissions.Add(Permissions.Endpoints.EndSession);
            descriptor.Permissions.Add(Permissions.ResponseTypes.Code);
            descriptor.Requirements.Add(Requirements.Features.ProofKeyForCodeExchange);
        }

        foreach (var grantType in client.GrantTypes)
        {
            descriptor.Permissions.Add(Permissions.Prefixes.GrantType + grantType);
        }

        foreach (var scope in client.Scopes.Where(scope => scope != Scopes.OpenId))
        {
            descriptor.Permissions.Add(Permissions.Prefixes.Scope + scope);
        }

        return descriptor;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "OAuth 클라이언트 {ClientId} 를 구성에 맞췄다")]
    private partial void LogSynchronized(string clientId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "구성에 없는 OAuth 클라이언트 {ClientId} 의 권한을 모두 비워 사용을 멈췄다 — 다시 쓰려면 구성에 넣는다")]
    private partial void LogDisabled(string clientId);
}
