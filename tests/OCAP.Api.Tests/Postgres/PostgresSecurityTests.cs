using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using OCAP.Api.Tests.Infrastructure;
using OCAP.Infrastructure.Persistence.Context;
using OCAP.Security.Abstractions;
using OCAP.Security.Domain.Entities;
using OCAP.Security.Infrastructure.Services;

namespace OCAP.Api.Tests.Postgres;

/// <summary>Fábrica que siempre usa PostgreSQL: estas pruebas verifican lo que la base en memoria no tiene.</summary>
public sealed class PostgresOcapApiFactory : OcapApiFactory
{
    protected override bool UsePostgres => true;
}

[Trait("Category", "Postgres")]
public class PostgresSecurityTests : IClassFixture<PostgresOcapApiFactory>
{
    private static readonly Guid SeedTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private readonly PostgresOcapApiFactory _factory;

    public PostgresSecurityTests(PostgresOcapApiFactory factory)
    {
        _factory = factory;
        _ = factory.Server; // crea la base y la siembra
    }

    [Fact]
    public async Task TheApiRole_IsNotSuperuser_DoesNotBypassRls_AndCannotChangeTheSchema()
    {
        await using var connection = await OpenAppConnectionAsync();

        var role = await ScalarAsync<string>(connection, "SELECT rolsuper::text || ',' || rolbypassrls::text FROM pg_roles WHERE rolname = current_user");
        role.Should().Be("false,false");

        var createTable = async () => await ScalarAsync<object>(connection, "CREATE TABLE intruso (id int)");
        await createTable.Should().ThrowAsync<PostgresException>().Where(e => e.SqlState == PostgresErrorCodes.InsufficientPrivilege);
    }

    [Fact]
    public async Task Rls_OnlyShowsTheRowsOfTheTenantSetOnTheConnection()
    {
        var otherTenant = await SeedOtherTenantAsync();
        await using var connection = await OpenAppConnectionAsync();

        // Sin organización ni bypass fijados, la conexión no ve nada (falla cerrado).
        (await ScalarAsync<long>(connection, "SELECT count(*) FROM \"UserIdentities\"")).Should().Be(0);

        await SetTenantAsync(connection, SeedTenantId);
        var visibleTenants = await ScalarAsync<long>(connection, "SELECT count(DISTINCT \"TenantId\") FROM \"UserIdentities\"");
        visibleTenants.Should().Be(1);
        (await ScalarAsync<long>(connection, $"SELECT count(*) FROM \"UserIdentities\" WHERE \"TenantId\" = '{otherTenant}'")).Should().Be(0);

        // Tampoco puede escribir filas de otra organización.
        var crossInsert = async () => await ScalarAsync<object>(connection,
            $"INSERT INTO \"Roles\" (\"Id\", \"TenantId\", \"Name\", \"Description\", \"Permissions\") " +
            $"VALUES ('{Guid.NewGuid()}', '{otherTenant}', 'X', 'X', '[]')");
        await crossInsert.Should().ThrowAsync<PostgresException>().Where(e => e.SqlState == "42501");
    }

    [Fact]
    public async Task Rls_BlocksAnotherTenantsRowsEvenWhenEfIgnoresItsQueryFilters()
    {
        var otherTenant = await SeedOtherTenantAsync();
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<ITenantScope>().Use(SeedTenantId);
        var db = scope.ServiceProvider.GetRequiredService<OCAPDbContext>();

        var tenants = await db.UserIdentities.IgnoreQueryFilters().Select(u => u.TenantId).Distinct().ToListAsync();

        tenants.Should().Equal(SeedTenantId);
        tenants.Should().NotContain(otherTenant);
    }

    [Fact]
    public async Task RefreshTokenRotation_TwoConcurrentRequestsWithTheSameToken_NeverYieldTwoSessions()
    {
        Guid userId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<OCAPDbContext>();
            userId = await db.UserIdentities.IgnoreQueryFilters().Where(u => u.Email == "admin@ocap.io").Select(u => u.Id).SingleAsync();
        }

        var issued = await WithServiceAsync(s => s.CreateRefreshTokenAsync(userId));
        var attempts = await Task.WhenAll(Enumerable.Range(0, 6).Select(_ => WithServiceAsync(s => s.ValidateAndRotateRefreshTokenAsync(issued.Token))));

        attempts.Count(a => a is not null).Should().BeLessThanOrEqualTo(1);
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<OCAPDbContext>();
            // Si hubo carrera se trata como reutilización: no queda ningún token vigente de la sesión.
            var active = await db.RefreshTokens.IgnoreQueryFilters().CountAsync(t => t.UserId == userId && !t.IsRevoked);
            active.Should().BeLessThanOrEqualTo(1);
        }
    }

    [Fact]
    public async Task LoginAndAuthenticatedRequests_WorkWithTheLimitedRoleAndRls()
    {
        var client = _factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/auth/login", new { email = "admin@ocap.io", password = "ChangeMe_Admin_2026!" });
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        var session = await login.Content.ReadFromJsonAsync<Session>();
        client.DefaultRequestHeaders.Authorization = new("Bearer", session!.AccessToken);

        (await client.GetAsync("/api/profile")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.PostAsync("/api/auth/refresh", null)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task<T> WithServiceAsync<T>(Func<IRefreshTokenService, Task<T>> action)
    {
        using var scope = _factory.Services.CreateScope();
        return await action(scope.ServiceProvider.GetRequiredService<IRefreshTokenService>());
    }

    private async Task<Guid> SeedOtherTenantAsync()
    {
        var tenantId = Guid.NewGuid();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OCAPDbContext>();
        var (hash, salt) = new PasswordHasher().HashPassword("Otra_Organizacion_2026!");
        db.Tenants.Add(new Tenant(tenantId, "Otra", $"otra-{tenantId:N}"));
        db.UserIdentities.Add(new UserIdentity(Guid.NewGuid(), tenantId, $"otra-{tenantId:N}@ocap.io", hash, salt, "Otra"));
        await db.SaveChangesAsync();
        return tenantId;
    }

    private async Task<NpgsqlConnection> OpenAppConnectionAsync()
    {
        var connection = new NpgsqlConnection(_factory.Postgres!.AppConnectionString);
        await connection.OpenAsync();
        return connection;
    }

    private static async Task SetTenantAsync(NpgsqlConnection connection, Guid tenantId)
    {
        await using var command = new NpgsqlCommand(
            "SELECT set_config('ocap.tenant_id', @t, false), set_config('ocap.bypass_rls', 'off', false)", connection);
        command.Parameters.AddWithValue("t", tenantId.ToString());
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<T> ScalarAsync<T>(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        return (T)(await command.ExecuteScalarAsync())!;
    }

    private sealed record Session(string AccessToken);
}
