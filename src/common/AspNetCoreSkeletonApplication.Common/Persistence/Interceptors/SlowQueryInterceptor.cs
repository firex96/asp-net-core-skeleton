using System.Data.Common;
using AspNetCoreSkeletonApplication.Common.Diagnostics;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AspNetCoreSkeletonApplication.Common.Persistence.Interceptors;

/// <summary>
/// <see cref="DiagnosticsOptions.SlowQueryThreshold"/> 를 넘긴 DB 명령을 경고로 남긴다 — 형제 스켈레톤의 @SlowMethod 자리.
/// SQL 은 매개변수 자리표시자 그대로 남고 값은 남지 않는다. 추적 ID 는 로깅 scope 로 함께 찍힌다.
/// </summary>
internal sealed partial class SlowQueryInterceptor(
    IOptionsMonitor<DiagnosticsOptions> options,
    ILogger<SlowQueryInterceptor> logger) : DbCommandInterceptor
{
    private const int MaxCommandLength = 1000;

    public override DbDataReader ReaderExecuted(DbCommand command, CommandExecutedEventData eventData, DbDataReader result)
    {
        Check(command, eventData);
        return result;
    }

    public override ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command, CommandExecutedEventData eventData, DbDataReader result, CancellationToken cancellationToken = default)
    {
        Check(command, eventData);
        return ValueTask.FromResult(result);
    }

    public override int NonQueryExecuted(DbCommand command, CommandExecutedEventData eventData, int result)
    {
        Check(command, eventData);
        return result;
    }

    public override ValueTask<int> NonQueryExecutedAsync(
        DbCommand command, CommandExecutedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        Check(command, eventData);
        return ValueTask.FromResult(result);
    }

    public override object? ScalarExecuted(DbCommand command, CommandExecutedEventData eventData, object? result)
    {
        Check(command, eventData);
        return result;
    }

    public override ValueTask<object?> ScalarExecutedAsync(
        DbCommand command, CommandExecutedEventData eventData, object? result, CancellationToken cancellationToken = default)
    {
        Check(command, eventData);
        return ValueTask.FromResult(result);
    }

    private void Check(DbCommand command, CommandExecutedEventData eventData)
    {
        if (eventData.Duration < options.CurrentValue.SlowQueryThreshold)
        {
            return;
        }

        var sql = command.CommandText.Length > MaxCommandLength
            ? string.Concat(command.CommandText.AsSpan(0, MaxCommandLength), "…")
            : command.CommandText;
        LogSlowQuery(logger, eventData.Duration.TotalMilliseconds, sql);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "느린 쿼리 {ElapsedMilliseconds:F0}ms — {CommandText}")]
    private static partial void LogSlowQuery(ILogger logger, double elapsedMilliseconds, string commandText);
}
