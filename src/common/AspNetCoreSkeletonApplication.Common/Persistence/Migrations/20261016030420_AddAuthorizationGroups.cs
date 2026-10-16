using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AspNetCoreSkeletonApplication.Common.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthorizationGroups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "role",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_role", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "role_permission",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<long>(type: "bigint", nullable: false),
                    Permission = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_role_permission", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "user_group",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_group", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "user_group_member",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserGroupId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_group_member", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "user_group_role",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserGroupId = table.Column<long>(type: "bigint", nullable: false),
                    RoleId = table.Column<long>(type: "bigint", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_group_role", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_role_Name",
                table: "role",
                column: "Name",
                unique: true,
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_role_permission_RoleId_Permission",
                table: "role_permission",
                columns: new[] { "RoleId", "Permission" },
                unique: true,
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_user_group_Name",
                table: "user_group",
                column: "Name",
                unique: true,
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_user_group_member_UserGroupId_UserId",
                table: "user_group_member",
                columns: new[] { "UserGroupId", "UserId" },
                unique: true,
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_user_group_member_UserId",
                table: "user_group_member",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_user_group_role_UserGroupId_RoleId",
                table: "user_group_role",
                columns: new[] { "UserGroupId", "RoleId" },
                unique: true,
                filter: "[DeletedAt] IS NULL");

            // 기존 Administrator 역할을 그룹 · 역할 · 권한으로 옮긴다 — 역할 테이블을 지우기 전에.
            migrationBuilder.Sql("""
                DECLARE @now datetimeoffset = SYSUTCDATETIME();
                INSERT INTO [role] ([Name], [Description], [CreatedBy], [CreatedAt], [UpdatedBy], [UpdatedAt])
                VALUES (N'Administrator', N'모든 권한', 0, @now, 0, @now);
                DECLARE @roleId bigint = SCOPE_IDENTITY();
                INSERT INTO [role_permission] ([RoleId], [Permission], [CreatedBy], [CreatedAt], [UpdatedBy], [UpdatedAt])
                VALUES (@roleId, N'products.manage', 0, @now, 0, @now),
                       (@roleId, N'authorization.manage', 0, @now, 0, @now);
                INSERT INTO [user_group] ([Name], [Description], [CreatedBy], [CreatedAt], [UpdatedBy], [UpdatedAt])
                VALUES (N'Administrators', N'운영 관리자', 0, @now, 0, @now);
                DECLARE @groupId bigint = SCOPE_IDENTITY();
                INSERT INTO [user_group_role] ([UserGroupId], [RoleId], [CreatedBy], [CreatedAt], [UpdatedBy], [UpdatedAt])
                VALUES (@groupId, @roleId, 0, @now, 0, @now);
                INSERT INTO [user_group_member] ([UserGroupId], [UserId], [CreatedBy], [CreatedAt], [UpdatedBy], [UpdatedAt])
                SELECT @groupId, userRole.[UserId], 0, @now, 0, @now
                FROM [identity_user_role] userRole
                JOIN [identity_role] identityRole ON identityRole.[Id] = userRole.[RoleId]
                WHERE identityRole.[NormalizedName] = N'ADMINISTRATOR';
                """);

            migrationBuilder.DropTable(
                name: "identity_role_claim");

            migrationBuilder.DropTable(
                name: "identity_user_role");

            migrationBuilder.DropTable(
                name: "identity_role");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "identity_role",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_identity_role", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "identity_role_claim",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RoleId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_identity_role_claim", x => x.Id);
                    table.ForeignKey(
                        name: "FK_identity_role_claim_identity_role_RoleId",
                        column: x => x.RoleId,
                        principalTable: "identity_role",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "identity_user_role",
                columns: table => new
                {
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    RoleId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_identity_user_role", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_identity_user_role_identity_role_RoleId",
                        column: x => x.RoleId,
                        principalTable: "identity_role",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_identity_user_role_identity_user_UserId",
                        column: x => x.UserId,
                        principalTable: "identity_user",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "identity_role",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[] { 1L, "7c9e6679-7425-40de-944b-e07fc1f90ae7", "Administrator", "ADMINISTRATOR" });

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "identity_role",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_identity_role_claim_RoleId",
                table: "identity_role_claim",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_identity_user_role_RoleId",
                table: "identity_user_role",
                column: "RoleId");

            // 권한 관리 권한을 가진 그룹의 소속을 Administrator 역할로 되돌린다 — 새 테이블을 지우기 전에.
            migrationBuilder.Sql("""
                INSERT INTO [identity_user_role] ([UserId], [RoleId])
                SELECT DISTINCT member.[UserId], 1
                FROM [user_group_member] member
                JOIN [user_group_role] groupRole ON groupRole.[UserGroupId] = member.[UserGroupId] AND groupRole.[DeletedAt] IS NULL
                JOIN [role_permission] rolePermission ON rolePermission.[RoleId] = groupRole.[RoleId] AND rolePermission.[DeletedAt] IS NULL
                WHERE member.[DeletedAt] IS NULL AND rolePermission.[Permission] = N'authorization.manage';
                """);

            migrationBuilder.DropTable(
                name: "role");

            migrationBuilder.DropTable(
                name: "role_permission");

            migrationBuilder.DropTable(
                name: "user_group");

            migrationBuilder.DropTable(
                name: "user_group_member");

            migrationBuilder.DropTable(
                name: "user_group_role");
        }
    }
}
