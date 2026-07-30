using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OCAP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantIsolation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OAuthConnections_UserId_Provider",
                table: "OAuthConnections");

            migrationBuilder.DropIndex(
                name: "IX_AgentToolPermissions_AgentId_PermissionName",
                table: "AgentToolPermissions");

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "WorkflowVersions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "WorkflowVariables",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "WorkflowExecutionHistories",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Users",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "ToolExecutions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Sessions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "RefreshTokens",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "OutboxMessages",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "OAuthConnections",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Messages",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Conversations",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "AiExecutionLogs",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "AiConversationMemories",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "AgentToolPermissions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Agents",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowVersions_TenantId",
                table: "WorkflowVersions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowVariables_TenantId",
                table: "WorkflowVariables",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowExecutionHistories_TenantId_ExecutionId",
                table: "WorkflowExecutionHistories",
                columns: new[] { "TenantId", "ExecutionId" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_TenantId",
                table: "Users",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_TenantId_DisplayName",
                table: "Users",
                columns: new[] { "TenantId", "DisplayName" });

            migrationBuilder.CreateIndex(
                name: "IX_ToolExecutions_TenantId",
                table: "ToolExecutions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ToolExecutions_TenantId_ExecutedAt",
                table: "ToolExecutions",
                columns: new[] { "TenantId", "ExecutedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_TenantId",
                table: "Sessions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_TenantId",
                table: "RefreshTokens",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_TenantId_UserId",
                table: "RefreshTokens",
                columns: new[] { "TenantId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_TenantId",
                table: "OutboxMessages",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_TenantId_ProcessedOnUtc",
                table: "OutboxMessages",
                columns: new[] { "TenantId", "ProcessedOnUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_OAuthConnections_TenantId",
                table: "OAuthConnections",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_OAuthConnections_TenantId_UserId_Provider",
                table: "OAuthConnections",
                columns: new[] { "TenantId", "UserId", "Provider" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Messages_TenantId",
                table: "Messages",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_TenantId_ConversationId",
                table: "Messages",
                columns: new[] { "TenantId", "ConversationId" });

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_TenantId",
                table: "Conversations",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_TenantId_UserId",
                table: "Conversations",
                columns: new[] { "TenantId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_AiExecutionLogs_TenantId_ExecutedAt",
                table: "AiExecutionLogs",
                columns: new[] { "TenantId", "ExecutedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AiConversationMemories_TenantId_ConversationId",
                table: "AiConversationMemories",
                columns: new[] { "TenantId", "ConversationId" });

            migrationBuilder.CreateIndex(
                name: "IX_AgentToolPermissions_TenantId",
                table: "AgentToolPermissions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentToolPermissions_TenantId_AgentId_PermissionName",
                table: "AgentToolPermissions",
                columns: new[] { "TenantId", "AgentId", "PermissionName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Agents_TenantId",
                table: "Agents",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Agents_TenantId_Name",
                table: "Agents",
                columns: new[] { "TenantId", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WorkflowVersions_TenantId",
                table: "WorkflowVersions");

            migrationBuilder.DropIndex(
                name: "IX_WorkflowVariables_TenantId",
                table: "WorkflowVariables");

            migrationBuilder.DropIndex(
                name: "IX_WorkflowExecutionHistories_TenantId_ExecutionId",
                table: "WorkflowExecutionHistories");

            migrationBuilder.DropIndex(
                name: "IX_Users_TenantId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_TenantId_DisplayName",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_ToolExecutions_TenantId",
                table: "ToolExecutions");

            migrationBuilder.DropIndex(
                name: "IX_ToolExecutions_TenantId_ExecutedAt",
                table: "ToolExecutions");

            migrationBuilder.DropIndex(
                name: "IX_Sessions_TenantId",
                table: "Sessions");

            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_TenantId",
                table: "RefreshTokens");

            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_TenantId_UserId",
                table: "RefreshTokens");

            migrationBuilder.DropIndex(
                name: "IX_OutboxMessages_TenantId",
                table: "OutboxMessages");

            migrationBuilder.DropIndex(
                name: "IX_OutboxMessages_TenantId_ProcessedOnUtc",
                table: "OutboxMessages");

            migrationBuilder.DropIndex(
                name: "IX_OAuthConnections_TenantId",
                table: "OAuthConnections");

            migrationBuilder.DropIndex(
                name: "IX_OAuthConnections_TenantId_UserId_Provider",
                table: "OAuthConnections");

            migrationBuilder.DropIndex(
                name: "IX_Messages_TenantId",
                table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_Messages_TenantId_ConversationId",
                table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_Conversations_TenantId",
                table: "Conversations");

            migrationBuilder.DropIndex(
                name: "IX_Conversations_TenantId_UserId",
                table: "Conversations");

            migrationBuilder.DropIndex(
                name: "IX_AiExecutionLogs_TenantId_ExecutedAt",
                table: "AiExecutionLogs");

            migrationBuilder.DropIndex(
                name: "IX_AiConversationMemories_TenantId_ConversationId",
                table: "AiConversationMemories");

            migrationBuilder.DropIndex(
                name: "IX_AgentToolPermissions_TenantId",
                table: "AgentToolPermissions");

            migrationBuilder.DropIndex(
                name: "IX_AgentToolPermissions_TenantId_AgentId_PermissionName",
                table: "AgentToolPermissions");

            migrationBuilder.DropIndex(
                name: "IX_Agents_TenantId",
                table: "Agents");

            migrationBuilder.DropIndex(
                name: "IX_Agents_TenantId_Name",
                table: "Agents");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "WorkflowVersions");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "WorkflowVariables");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "WorkflowExecutionHistories");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "ToolExecutions");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "OAuthConnections");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "AiExecutionLogs");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "AiConversationMemories");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "AgentToolPermissions");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Agents");

            migrationBuilder.CreateIndex(
                name: "IX_OAuthConnections_UserId_Provider",
                table: "OAuthConnections",
                columns: new[] { "UserId", "Provider" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AgentToolPermissions_AgentId_PermissionName",
                table: "AgentToolPermissions",
                columns: new[] { "AgentId", "PermissionName" },
                unique: true);
        }
    }
}
