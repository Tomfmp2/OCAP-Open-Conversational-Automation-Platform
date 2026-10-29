using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OCAP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AssistantConversations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AssistantConversations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssistantConversations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AssistantMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssistantMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssistantMessages_AssistantConversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "AssistantConversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssistantConversations_TenantId_UserId_UpdatedAt",
                table: "AssistantConversations",
                columns: new[] { "TenantId", "UserId", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AssistantMessages_ConversationId_CreatedAt",
                table: "AssistantMessages",
                columns: new[] { "ConversationId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssistantMessages");

            migrationBuilder.DropTable(
                name: "AssistantConversations");
        }
    }
}
