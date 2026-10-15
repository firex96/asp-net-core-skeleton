using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AspNetCoreSkeletonApplication.Common.Persistence.Migrations
{
    /// <summary>
    /// Batch 앱의 Quartz.NET 스케줄러 테이블을 <c>quartz</c> 스키마에 만든다. 프레임워크 정본 DDL(Scripts/quartz-4.5.0-sqlserver.sql)을
    /// 그대로 실행하므로 업무 테이블 규칙(FOREIGN KEY 금지 등)의 대상이 아니다. Batch 앱이 없는 프로젝트에서도 테이블만 생긴다(쓰지 않는다).
    /// </summary>
    public partial class AddQuartzScheduler : Migration
    {
        private const string Schema = "quartz";
        private const string Script = "quartz-4.5.0-sqlserver.sql";

        // 외래 키를 고려해 자식 테이블부터 지운다.
        private static readonly string[] Tables =
        [
            "QRTZ_EXECUTION_HISTORY", "QRTZ_MISFIRE_HISTORY", "QRTZ_JOB_STATUS", "QRTZ_CALENDARS",
            "QRTZ_CRON_TRIGGERS", "QRTZ_SIMPLE_TRIGGERS", "QRTZ_SIMPROP_TRIGGERS", "QRTZ_BLOB_TRIGGERS",
            "QRTZ_FIRED_TRIGGERS", "QRTZ_PAUSED_TRIGGER_GRPS", "QRTZ_PAUSED_JOB_GRPS", "QRTZ_SCHEDULER_STATE",
            "QRTZ_LOCKS", "QRTZ_TRIGGERS", "QRTZ_JOB_DETAILS",
        ];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(Schema);

            foreach (var batch in ReadScriptBatches())
            {
                migrationBuilder.Sql(batch.Replace("[dbo].", $"[{Schema}].", StringComparison.Ordinal));
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var table in Tables)
            {
                migrationBuilder.DropTable(name: table, schema: Schema);
            }

            migrationBuilder.Sql($"DROP SCHEMA [{Schema}]");
        }

        /// <summary>스크립트를 GO 줄 단위로 나눈다 — EF 의 Sql() 은 한 배치씩 실행한다.</summary>
        private static IEnumerable<string> ReadScriptBatches()
        {
            var assembly = typeof(AddQuartzScheduler).Assembly;
            var resource = assembly.GetManifestResourceNames().Single(name => name.EndsWith(Script, StringComparison.Ordinal));
            using var reader = new StreamReader(assembly.GetManifestResourceStream(resource)!);

            var batch = new System.Text.StringBuilder();
            while (reader.ReadLine() is { } line)
            {
                if (line.Trim().Equals("GO", StringComparison.OrdinalIgnoreCase))
                {
                    if (batch.ToString().Trim().Length > 0)
                    {
                        yield return batch.ToString();
                    }

                    batch.Clear();
                    continue;
                }

                batch.AppendLine(line);
            }

            if (batch.ToString().Trim().Length > 0)
            {
                yield return batch.ToString();
            }
        }
    }
}
