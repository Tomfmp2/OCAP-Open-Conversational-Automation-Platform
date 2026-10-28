using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;

namespace OCAP.Infrastructure.Persistence.Tenancy;

/// <summary>
/// Segunda barrera del aislamiento entre organizaciones, en la propia base: cada tabla con TenantId tiene una política
/// RLS que solo deja ver y escribir filas de la organización fijada en la conexión
/// (<see cref="Interceptors.TenantRlsConnectionInterceptor"/>). Se aplica después de cada migración y recorre las tablas
/// existentes, así una tabla nueva queda protegida sin acordarse de escribir su política.
/// Los superusuarios (el dueño que migra) ignoran RLS: la política vale para el rol con el que corre la API.
/// </summary>
public static partial class RowLevelSecurity
{
    public const string PolicyName = "ocap_tenant_isolation";

    // Una conexión que no fijó ocap.bypass_rls (current_setting = NULL) no ve nada: falla cerrado. Va dentro de un
    // literal de format(), por eso las comillas simples están duplicadas.
    private const string Predicate =
        "current_setting(''ocap.bypass_rls'', true) = ''on'' OR \"TenantId\" = NULLIF(current_setting(''ocap.tenant_id'', true), '''')::uuid";

    // Solo constantes: este SQL no lleva datos externos.
    private const string ApplyPoliciesSql = $$"""
            DO $$
            DECLARE t text;
            BEGIN
              FOR t IN
                SELECT c.table_name
                FROM information_schema.columns c
                JOIN information_schema.tables tb ON tb.table_schema = c.table_schema AND tb.table_name = c.table_name
                WHERE c.table_schema = 'public' AND c.column_name = 'TenantId' AND tb.table_type = 'BASE TABLE'
              LOOP
                EXECUTE format('ALTER TABLE %I ENABLE ROW LEVEL SECURITY', t);
                EXECUTE format('DROP POLICY IF EXISTS {{PolicyName}} ON %I', t);
                EXECUTE format('CREATE POLICY {{PolicyName}} ON %I USING ({{Predicate}}) WITH CHECK ({{Predicate}})', t);
              END LOOP;
            END $$;
            """;

    public static Task ApplyPoliciesAsync(DbContext context, CancellationToken cancellationToken = default) =>
        context.Database.ExecuteSqlRawAsync(ApplyPoliciesSql, cancellationToken);

    /// <summary>
    /// Crea (o actualiza) el rol de la API: puede leer y escribir datos, pero no es superusuario, no ignora RLS y no
    /// puede cambiar el esquema. Una inyección SQL a través de la API queda limitada a eso.
    /// </summary>
    public static async Task ProvisionAppRoleAsync(DbContext context, string role, string password, CancellationToken cancellationToken = default)
    {
        if (!RoleName().IsMatch(role))
            throw new InvalidOperationException($"Nombre de rol de base de datos inválido: '{role}'.");
        if (string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("Falta la contraseña del rol de base de datos de la API.");

        // Nombre y contraseña llegan como parámetros a set_config y el bloque los cita con format(%I / %L). La conexión
        // se mantiene abierta: los valores de set_config viven en la sesión y el pool la reinicia al devolverla.
        await context.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            await context.Database.ExecuteSqlRawAsync(
                "SELECT set_config('ocap.app_role', {0}, false), set_config('ocap.app_password', {1}, false)",
                new object[] { role, password }, cancellationToken);

            await context.Database.ExecuteSqlRawAsync("""
                DO $$
                DECLARE r text := current_setting('ocap.app_role');
                BEGIN
                  IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = r) THEN
                    EXECUTE format('CREATE ROLE %I LOGIN', r);
                  END IF;
                  EXECUTE format('ALTER ROLE %I WITH LOGIN NOSUPERUSER NOBYPASSRLS NOCREATEDB NOCREATEROLE NOREPLICATION PASSWORD %L',
                                 r, current_setting('ocap.app_password'));
                  EXECUTE format('GRANT CONNECT ON DATABASE %I TO %I', current_database(), r);
                  EXECUTE format('GRANT USAGE ON SCHEMA public TO %I', r);
                  EXECUTE format('GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO %I', r);
                  EXECUTE format('GRANT USAGE, SELECT, UPDATE ON ALL SEQUENCES IN SCHEMA public TO %I', r);
                  EXECUTE format('REVOKE INSERT, UPDATE, DELETE ON TABLE "__EFMigrationsHistory" FROM %I', r);
                END $$;
                SELECT set_config('ocap.app_password', '', false);
                """, cancellationToken);
        }
        finally
        {
            await context.Database.CloseConnectionAsync();
        }
    }

    [GeneratedRegex("^[a-z_][a-z0-9_]{0,62}$")]
    private static partial Regex RoleName();
}
