using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OCAP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AllowSeveralChannelsPerType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ChannelConnections_TenantId_Provider",
                table: "ChannelConnections");

            migrationBuilder.CreateIndex(
                name: "IX_ChannelConnections_TenantId_Provider",
                table: "ChannelConnections",
                columns: new[] { "TenantId", "Provider" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ChannelConnections_TenantId_Provider",
                table: "ChannelConnections");

            migrationBuilder.CreateIndex(
                name: "IX_ChannelConnections_TenantId_Provider",
                table: "ChannelConnections",
                columns: new[] { "TenantId", "Provider" },
                unique: true);
        }
    }
}
