using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace AspNetCoreSkeletonApplication.Common.Web.OAuth;

/// <summary>
/// 구성(<c>Authentication:OAuthServer:Clients</c>)의 클라이언트를 기동할 때 저장소에 맞춘다 — 없으면 만들고 있으면 구성대로 고친다.
/// 클라이언트의 정본은 구성이고, 관리 화면은 두지 않는다. 구성에서 빠진 클라이언트는 지우지 않는다(이미 발급한 토큰 · 동의가 남아 있다).
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
}
