using AspNetCoreSkeletonApplication.Common.Configuration;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;

namespace AspNetCoreSkeletonApplication.Common.Web;

/// <summary>Data Protection 키 저장소의 Redis 연결. 키 링을 처음 읽을 때 연결한다(연결 문자열이 없으면 그때 멈춘다).</summary>
internal sealed class DataProtectionRedis(IConfiguration configuration) : IDisposable
{
    private readonly Lazy<ConnectionMultiplexer> _connection =
        new(() => ConnectionMultiplexer.Connect(configuration.GetRequiredConnectionString("Redis")));

    public IDatabase Database => _connection.Value.GetDatabase();

    public void Dispose()
    {
        if (_connection.IsValueCreated)
        {
            _connection.Value.Dispose();
        }
    }
}
