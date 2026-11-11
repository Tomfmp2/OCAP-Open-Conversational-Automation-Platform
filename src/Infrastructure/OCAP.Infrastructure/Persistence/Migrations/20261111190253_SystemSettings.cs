using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OCAP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SystemSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSystemAdmin",
                table: "UserIdentities",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "SystemSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    RateLimitingEnabled = table.Column<bool>(type: "boolean", nullable: true),
                    RequestsPerMinute = table.Column<int>(type: "integer", nullable: true),
                    AuthAttemptsPerMinute = table.Column<int>(type: "integer", nullable: true),
                    WidgetMessagesPerMinute = table.Column<int>(type: "integer", nullable: true),
                    WidgetReadsPerMinute = table.Column<int>(type: "integer", nullable: true),
                    SessionMinutes = table.Column<int>(type: "integer", nullable: true),
                    AllowedOrigins = table.Column<List<string>>(type: "text[]", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemSettings", x => x.Id);
                });

            // Instalaciones que ya tienen usuarios: la sección Sistema queda en manos del administrador más antiguo,
            // que es quien instaló. Sin esto nadie podría abrirla.
            migrationBuilder.Sql("""
                UPDATE "UserIdentities" SET "IsSystemAdmin" = TRUE
                WHERE "Id" = (
                    SELECT u."Id"
                    FROM "UserIdentities" u
                    JOIN "UserRoles" ur ON ur."UserId" = u."Id"
                    JOIN "Roles" r ON r."Id" = ur."RoleId"
                    WHERE r."Name" = 'Admin'
                    ORDER BY u."CreatedAtUtc", u."Id"
                    LIMIT 1);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "IsSystemAdmin",
                table: "UserIdentities");
        }
    }
}
