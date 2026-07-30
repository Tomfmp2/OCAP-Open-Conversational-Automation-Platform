using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OCAP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkflowRuntimeEnterprise : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CompensationJson",
                table: "WorkflowExecutions",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "ResumePayloadJson",
                table: "WorkflowExecutions",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WaitSignal",
                table: "WorkflowExecutions",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "WaitUntilUtc",
                table: "WorkflowExecutions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WorkflowVersionNumber",
                table: "WorkflowExecutions",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowExecutions_Status_WaitUntilUtc",
                table: "WorkflowExecutions",
                columns: new[] { "Status", "WaitUntilUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WorkflowExecutions_Status_WaitUntilUtc",
                table: "WorkflowExecutions");

            migrationBuilder.DropColumn(
                name: "CompensationJson",
                table: "WorkflowExecutions");

            migrationBuilder.DropColumn(
                name: "ResumePayloadJson",
                table: "WorkflowExecutions");

            migrationBuilder.DropColumn(
                name: "WaitSignal",
                table: "WorkflowExecutions");

            migrationBuilder.DropColumn(
                name: "WaitUntilUtc",
                table: "WorkflowExecutions");

            migrationBuilder.DropColumn(
                name: "WorkflowVersionNumber",
                table: "WorkflowExecutions");
        }
    }
}
