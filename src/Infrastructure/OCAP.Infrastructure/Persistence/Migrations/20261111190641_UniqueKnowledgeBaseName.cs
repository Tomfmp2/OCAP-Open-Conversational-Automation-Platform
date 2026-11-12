using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OCAP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UniqueKnowledgeBaseName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // La API ya rechaza nombres repetidos, pero dos peticiones a la vez pasaban la comprobación. Las colecciones
            // que ya estén repetidas se renombran («Precios (2)») para poder crear el índice sin perder ninguna.
            migrationBuilder.Sql("""
                UPDATE "KnowledgeBases" kb
                SET "Name" = left(kb."Name", 240) || ' (' || d.rn || ')'
                FROM (
                    SELECT "Id", row_number() OVER (PARTITION BY "TenantId", lower("Name") ORDER BY "CreatedAtUtc", "Id") AS rn
                    FROM "KnowledgeBases") d
                WHERE d."Id" = kb."Id" AND d.rn > 1;

                CREATE UNIQUE INDEX "IX_KnowledgeBases_TenantId_NameLower"
                ON "KnowledgeBases" ("TenantId", lower("Name"));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_KnowledgeBases_TenantId_NameLower";""");
        }
    }
}
