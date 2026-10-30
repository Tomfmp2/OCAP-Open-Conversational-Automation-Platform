using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OCAP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UniqueTelegramBot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Un bot de Telegram en una sola conexión de todo OCAP: dos conexiones se pelearían sus mensajes
            // (Telegram entrega cada update una vez). La metadata se guarda como JSON en texto.
            migrationBuilder.Sql("""
                CREATE UNIQUE INDEX "IX_ChannelConnections_TelegramBotId"
                ON "ChannelConnections" ((("ConfigurationMetadata")::jsonb ->> 'BotId'))
                WHERE "Provider" = 'Telegram' AND ("ConfigurationMetadata")::jsonb ? 'BotId';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_ChannelConnections_TelegramBotId";""");
        }
    }
}
