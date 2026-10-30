using Microsoft.EntityFrameworkCore;
using Npgsql;
using OCAP.Infrastructure.Persistence.Context;
using OCAP.Infrastructure.Persistence.Tenancy;
using Testcontainers.PostgreSql;

namespace OCAP.Api.Tests.Infrastructure;

/// <summary>
/// Un PostgreSQL con pgvector (la misma imagen del stack) para toda la ejecución. Cada base de prueba se migra con el
/// dueño, como ocap-migrate, y la API se conecta con el rol limitado: así las pruebas ejercitan RLS y los permisos
/// reales, que la base en memoria de EF no tiene.
/// </summary>
public static class TestPostgres
{
    public const string AppRole = "ocap_app";
    public const string AppPassword = "ocap_app_test_password";

    /// <summary>OCAP_TEST_DATABASE=postgres corre toda la suite de la API contra PostgreSQL en vez de la base en memoria.</summary>
    public static bool UseForWholeSuite =>
        string.Equals(Environment.GetEnvironmentVariable("OCAP_TEST_DATABASE"), "postgres", StringComparison.OrdinalIgnoreCase);

    private static readonly SemaphoreSlim StartLock = new(1, 1);
    private static PostgreSqlContainer? _container;

    public sealed record Database(string OwnerConnectionString, string AppConnectionString);

    /// <summary>Crea una base nueva, la migra, aplica RLS y da permisos al rol de la API.</summary>
    public static async Task<Database> CreateDatabaseAsync()
    {
        var container = await StartAsync();
        var name = $"ocap_test_{Guid.NewGuid():N}";

        await using (var admin = new NpgsqlConnection(container.GetConnectionString()))
        {
            await admin.OpenAsync();
            await using var create = new NpgsqlCommand($"CREATE DATABASE \"{name}\"", admin);
            await create.ExecuteNonQueryAsync();
        }

        // El dueño solo migra: sin pool, no deja conexiones abiertas.
        var owner = new NpgsqlConnectionStringBuilder(container.GetConnectionString()) { Database = name, Pooling = false }.ConnectionString;
        var options = new DbContextOptionsBuilder<OCAPDbContext>()
            .UseNpgsql(owner, o => o.UseVector().MigrationsAssembly(typeof(OCAPDbContext).Assembly.FullName))
            .Options;
        await using (var db = new OCAPDbContext(options))
        {
            await db.Database.ExecuteSqlRawAsync("CREATE EXTENSION IF NOT EXISTS vector");
            await db.Database.MigrateAsync();
            await RowLevelSecurity.ApplyPoliciesAsync(db);
            await RowLevelSecurity.ProvisionAppRoleAsync(db, AppRole, AppPassword);
        }

        var app = new NpgsqlConnectionStringBuilder(owner) { Username = AppRole, Password = AppPassword, Pooling = true, MaxPoolSize = 10 }.ConnectionString;
        return new Database(owner, app);
    }

    private static async Task<PostgreSqlContainer> StartAsync()
    {
        if (_container is not null) return _container;
        await StartLock.WaitAsync();
        try
        {
            if (_container is null)
            {
                // Cada fábrica de pruebas abre su base y su pool, y siguen vivas hasta el final de la ejecución: con el
                // límite por defecto (100) las últimas fallaban de vez en cuando con "too many clients".
                var container = new PostgreSqlBuilder("pgvector/pgvector:pg16")
                    .WithDatabase("postgres")
                    .WithCommand("-c", "max_connections=500")
                    .Build();
                await container.StartAsync();
                _container = container;
            }
            return _container;
        }
        finally
        {
            StartLock.Release();
        }
    }
}
