using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AspNetCoreSkeletonApplication.Common.Persistence.Migrations
{
    /// <summary>
    /// 이메일 확인(<c>RequireConfirmedEmail</c>)을 켜기 전에 가입한 계정은 확인된 것으로 둔다 — 그 계정들은 확인 메일을 받은 적이 없어서,
    /// 그대로 두면 아무도 로그인하지 못한다.
    /// </summary>
    public partial class ConfirmExistingEmails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE identity_user SET EmailConfirmed = 1 WHERE EmailConfirmed = 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 되돌리지 않는다 — 어느 계정이 원래 확인 전이었는지 남아 있지 않다.
        }
    }
}
