using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OCAP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeveralAiConfigurationsPerProvider : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AiProviderConfigurations_TenantId_ProviderName",
                table: "AiProviderConfigurations");

            migrationBuilder.CreateIndex(
                name: "IX_AiProviderConfigurations_TenantId_DisplayName",
                table: "AiProviderConfigurations",
                columns: new[] { "TenantId", "DisplayName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AiProviderConfigurations_TenantId_ProviderName",
                table: "AiProviderConfigurations",
                columns: new[] { "TenantId", "ProviderName" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AiProviderConfigurations_TenantId_DisplayName",
                table: "AiProviderConfigurations");

            migrationBuilder.DropIndex(
                name: "IX_AiProviderConfigurations_TenantId_ProviderName",
                table: "AiProviderConfigurations");

            migrationBuilder.CreateIndex(
                name: "IX_AiProviderConfigurations_TenantId_ProviderName",
                table: "AiProviderConfigurations",
                columns: new[] { "TenantId", "ProviderName" },
                unique: true);
        }
    }
}
