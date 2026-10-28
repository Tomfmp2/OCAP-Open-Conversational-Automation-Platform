using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OCAP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantPreferredAiProvider : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PreferredAiProvider",
                table: "Tenants",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PreferredAiProvider",
                table: "Tenants");
        }
    }
}
