using System.Text.RegularExpressions;
using AspNetCoreSkeletonApplication.Common.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace AspNetCoreSkeletonApplication.UnitTests.Conventions;

/// <summary>
/// 테이블 이름 규칙 (예외 없음) — 이름은 클래스 이름에서 기계적으로 정한다. 도메인은 테이블 이름에 넣지 않는다:
/// 도메인 경계가 바뀌어도 운영 DB 이름(리포트 · 연동이 의존한다)은 그대로 둔다.
/// <list type="bullet">
/// <item>업무 Entity: 클래스 이름에서 <c>Entity</c> 를 뺀 snake_case (<c>ProductStatusHistoryEntity</c> → <c>product_status_history</c>).</item>
/// <item>프레임워크가 정의한 엔티티: 그 프레임워크 이름 접두사(<c>identity_</c> · OpenIddict <c>oauth_</c>). Quartz 는 별도 <c>quartz</c> 스키마라 모델 밖이다.</item>
/// <item>어느 쪽이든 SQL Server 예약어를 테이블 이름으로 쓰지 않는다.</item>
/// </list>
/// </summary>
public partial class TableNamingConventionTests
{
    private static readonly string[] FrameworkTablePrefixes = ["identity_", "oauth_"];

    // https://learn.microsoft.com/sql/t-sql/language-elements/reserved-keywords-transact-sql
    private static readonly HashSet<string> ReservedWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "add", "all", "alter", "and", "any", "as", "asc", "authorization", "backup", "begin", "between", "break", "browse", "bulk", "by",
        "cascade", "case", "check", "checkpoint", "close", "clustered", "coalesce", "collate", "column", "commit", "compute", "constraint",
        "contains", "containstable", "continue", "convert", "create", "cross", "current", "current_date", "current_time", "current_timestamp",
        "current_user", "cursor", "database", "dbcc", "deallocate", "declare", "default", "delete", "deny", "desc", "disk", "distinct",
        "distributed", "double", "drop", "dump", "else", "end", "errlvl", "escape", "except", "exec", "execute", "exists", "exit", "external",
        "fetch", "file", "fillfactor", "for", "foreign", "freetext", "freetexttable", "from", "full", "function", "goto", "grant", "group",
        "having", "holdlock", "identity", "identity_insert", "identitycol", "if", "in", "index", "inner", "insert", "intersect", "into", "is",
        "join", "key", "kill", "left", "like", "lineno", "load", "merge", "national", "nocheck", "nonclustered", "not", "null", "nullif", "of",
        "off", "offsets", "on", "open", "opendatasource", "openquery", "openrowset", "openxml", "option", "or", "order", "outer", "over",
        "percent", "pivot", "plan", "precision", "primary", "print", "proc", "procedure", "public", "raiserror", "read", "readtext",
        "reconfigure", "references", "replication", "restore", "restrict", "return", "revert", "revoke", "right", "rollback", "rowcount",
        "rowguidcol", "rule", "save", "schema", "securityaudit", "select", "semantickeyphrasetable", "semanticsimilaritydetailstable",
        "semanticsimilaritytable", "session_user", "set", "setuser", "shutdown", "some", "statistics", "system_user", "table", "tablesample",
        "textsize", "then", "to", "top", "tran", "transaction", "trigger", "truncate", "try_convert", "tsequal", "union", "unique", "unpivot",
        "update", "updatetext", "use", "user", "values", "varying", "view", "waitfor", "when", "where", "while", "with", "writetext",
    };

    [Fact]
    public void Tables_FollowClassNameOrFrameworkPrefix_AndAvoidReservedWords()
    {
        var violations = new List<string>();
        foreach (var entityType in DesignTimeModel().GetEntityTypes().Where(entityType => entityType.GetTableName() is not null))
        {
            var table = entityType.GetTableName()!;
            var clrType = entityType.ClrType;
            if (ConventionScan.LayerOf(clrType) == Layer.Entity)
            {
                var expected = SnakeCase(clrType.Name[..^"Entity".Length]);
                if (table != expected)
                {
                    violations.Add($"{clrType.Name} — 테이블 '{table}' 은 '{expected}' 여야 한다(클래스 이름의 snake_case)");
                }
            }
            else if (!FrameworkTablePrefixes.Any(prefix => table.StartsWith(prefix, StringComparison.Ordinal)))
            {
                violations.Add($"{clrType.FullName} — 프레임워크 엔티티의 테이블 '{table}' 은 프레임워크 접두사({string.Join(", ", FrameworkTablePrefixes)})로 시작한다");
            }

            if (ReservedWords.Contains(table))
            {
                violations.Add($"{clrType.Name} — 테이블 '{table}' 은 SQL Server 예약어다");
            }
        }

        ConventionScan.AssertNoViolations(violations);
    }

    [Theory]
    [InlineData("Product", "product")]
    [InlineData("ProductStatusHistory", "product_status_history")]
    [InlineData("UserGroup", "user_group")]
    public void SnakeCase_SplitsPascalCaseWords(string name, string expected) => Assert.Equal(expected, SnakeCase(name));

    private static IModel DesignTimeModel()
    {
        // 연결하지 않는다 — 모델만 만든다.
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer("Server=model-only").Options;
        using var context = new AppDbContext(options);
        return context.GetService<IDesignTimeModel>().Model;
    }

    private static string SnakeCase(string name) => WordBoundary().Replace(name, "_$1").ToLowerInvariant();

    [GeneratedRegex("(?<=[a-z0-9])([A-Z])")]
    private static partial Regex WordBoundary();
}
