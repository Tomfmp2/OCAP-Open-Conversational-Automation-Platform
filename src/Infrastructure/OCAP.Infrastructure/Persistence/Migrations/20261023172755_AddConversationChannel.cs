using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OCAP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddConversationChannel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Channel",
                table: "Conversations",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ChannelConnectionId",
                table: "Conversations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalContactId",
                table: "Conversations",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_TenantId_LastActivityAt",
                table: "Conversations",
                columns: new[] { "TenantId", "LastActivityAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Conversations_TenantId_LastActivityAt",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "Channel",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "ChannelConnectionId",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "ExternalContactId",
                table: "Conversations");
        }
    }
}
