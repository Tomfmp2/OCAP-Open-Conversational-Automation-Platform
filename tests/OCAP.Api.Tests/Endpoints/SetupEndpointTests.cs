using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OCAP.Api.Setup;
using OCAP.Api.Tests.Infrastructure;
using OCAP.Infrastructure.Persistence.Context;

namespace OCAP.Api.Tests.Endpoints;

/// <summary>Instalación nueva: base sin usuarios, como el primer arranque con Docker.</summary>
public sealed class EmptyOcapApiFactory : OcapApiFactory
{
    protected override bool SeedTestData => false;
}

/// <summary>Despliegue automatizado: el admin llega por variables de entorno.</summary>
public sealed class BootstrapOcapApiFactory : OcapApiFactory
{
    protected override bool SeedTestData => false;

    protected override IDictionary<string, string?> ExtraSettings => new Dictionary<string, string?>
    {
        ["Bootstrap:Enabled"] = "true",
        ["Bootstrap:AdminEmail"] = "ci-admin@ocap.io",
        ["Bootstrap:AdminPassword"] = "Ci_Admin_Password_2026",
        ["Bootstrap:TenantName"] = "Panadería Sol",
        ["Bootstrap:TenantSlug"] = "panaderia-sol",
        ["Bootstrap:AdminName"] = "Ana Pérez"
    };
}

public class SetupEndpointTests
{
    private static SetupRequest ValidRequest(string code) => new(
        SetupCode: code,
        OrganizationName: "Acme Logística S.A.",
        AdminName: "Ana Pérez",
        AdminEmail: "Ana@Acme.io",
        AdminPassword: "Segura_Clave_2026");

    private static string CodeOf(OcapApiFactory factory) =>
        factory.Services.GetRequiredService<SetupCodeService>().CurrentCode
        ?? throw new InvalidOperationException("La API no emitió código de configuración.");

    private static async Task<int> CountUsersAsync(OcapApiFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OCAPDbContext>();
        return await db.UserIdentities.IgnoreQueryFilters().CountAsync();
    }

    [Fact]
    public async Task Status_OnEmptyInstallation_IsRequiredAndAnnouncesACode()
    {
        using var factory = new EmptyOcapApiFactory();
        var client = factory.CreateClient();

        var status = await client.GetFromJsonAsync<SetupStatusResponse>("/api/setup/status");

        status!.Required.Should().BeTrue();
        CodeOf(factory).Should().MatchRegex("^[A-Z2-9]{4}-[A-Z2-9]{4}-[A-Z2-9]{4}$");
    }

    [Fact]
    public async Task Complete_WithValidCode_CreatesAdminAndReturnsASession()
    {
        using var factory = new EmptyOcapApiFactory();
        var client = factory.CreateClient();
        await client.GetAsync("/api/setup/status");

        var response = await client.PostAsJsonAsync("/api/setup", ValidRequest(CodeOf(factory)));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var session = await response.Content.ReadFromJsonAsync<SessionPayload>();
        session!.AccessToken.Should().NotBeNullOrWhiteSpace();
        session.Email.Should().Be("ana@acme.io");
        session.RoleName.Should().Be("Admin");

        var login = await client.PostAsJsonAsync("/api/auth/login", new { email = "ana@acme.io", password = "Segura_Clave_2026" });
        login.StatusCode.Should().Be(HttpStatusCode.OK);

        var status = await client.GetFromJsonAsync<SetupStatusResponse>("/api/setup/status");
        status!.Required.Should().BeFalse();
        factory.Services.GetRequiredService<SetupCodeService>().CurrentCode.Should().BeNull();
    }

    [Fact]
    public async Task Complete_AcceptsTheCodeWithoutDashesAndInLowercase()
    {
        using var factory = new EmptyOcapApiFactory();
        var client = factory.CreateClient();
        await client.GetAsync("/api/setup/status");
        var typed = CodeOf(factory).Replace("-", string.Empty).ToLowerInvariant();

        var response = await client.PostAsJsonAsync("/api/setup", ValidRequest(typed));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Complete_WithWrongCode_IsForbiddenAndCreatesNothing()
    {
        using var factory = new EmptyOcapApiFactory();
        var client = factory.CreateClient();
        await client.GetAsync("/api/setup/status");

        var response = await client.PostAsJsonAsync("/api/setup", ValidRequest("AAAA-BBBB-CCCC"));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await CountUsersAsync(factory)).Should().Be(0);
    }

    [Fact]
    public async Task Complete_Twice_SecondAttemptConflicts()
    {
        using var factory = new EmptyOcapApiFactory();
        var client = factory.CreateClient();
        await client.GetAsync("/api/setup/status");
        var code = CodeOf(factory);

        (await client.PostAsJsonAsync("/api/setup", ValidRequest(code))).StatusCode.Should().Be(HttpStatusCode.OK);
        var second = await client.PostAsJsonAsync("/api/setup", ValidRequest(code) with { AdminEmail = "intruso@x.io" });

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await CountUsersAsync(factory)).Should().Be(1);
    }

    [Fact]
    public async Task Complete_InParallel_CreatesASingleAdmin()
    {
        using var factory = new EmptyOcapApiFactory();
        var client = factory.CreateClient();
        await client.GetAsync("/api/setup/status");
        var code = CodeOf(factory);

        var responses = await Task.WhenAll(Enumerable.Range(0, 5).Select(i =>
            client.PostAsJsonAsync("/api/setup", ValidRequest(code) with { AdminEmail = $"admin{i}@acme.io" })));

        responses.Count(r => r.StatusCode == HttpStatusCode.OK).Should().Be(1);
        (await CountUsersAsync(factory)).Should().Be(1);
    }

    [Fact]
    public async Task Complete_WithWeakPassword_ReturnsFieldErrors()
    {
        using var factory = new EmptyOcapApiFactory();
        var client = factory.CreateClient();
        await client.GetAsync("/api/setup/status");

        var response = await client.PostAsJsonAsync("/api/setup", ValidRequest(CodeOf(factory)) with { AdminPassword = "corta" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Contain("AdminPassword");
        (await CountUsersAsync(factory)).Should().Be(0);
    }

    [Fact]
    public async Task ExistingInstallation_IsNotRequired_AndRejectsSetup()
    {
        using var factory = new OcapApiFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/setup", ValidRequest("AAAA-BBBB-CCCC"));
        var status = await client.GetFromJsonAsync<SetupStatusResponse>("/api/setup/status");

        status!.Required.Should().BeFalse();
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        factory.Services.GetRequiredService<SetupCodeService>().CurrentCode.Should().BeNull();
    }

    [Fact]
    public async Task BootstrapFromEnvironment_CreatesTheAdmin_AndSkipsSetup()
    {
        using var factory = new BootstrapOcapApiFactory();
        var client = factory.CreateClient();

        var status = await client.GetFromJsonAsync<SetupStatusResponse>("/api/setup/status");
        var login = await client.PostAsJsonAsync("/api/auth/login", new { email = "ci-admin@ocap.io", password = "Ci_Admin_Password_2026" });

        status!.Required.Should().BeFalse();
        login.StatusCode.Should().Be(HttpStatusCode.OK);

        // ocap install pasa el nombre de la organización y del admin: no quedan "OCAP" ni "Administrator".
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OCAPDbContext>();
        var admin = await db.UserIdentities.IgnoreQueryFilters().SingleAsync();
        var tenant = await db.Tenants.SingleAsync(t => t.Id == admin.TenantId);
        admin.FullName.Should().Be("Ana Pérez");
        tenant.Name.Should().Be("Panadería Sol");
    }

    [Theory]
    [InlineData("Acme Logística S.A.", "acme-logistica-s-a")]
    [InlineData("  Ñandú & Co  ", "nandu-co")]
    public void OrganizationSlug_IsDerivedFromTheName(string name, string expected)
    {
        SetupRequestValidator.ToSlug(name).Should().Be(expected);
    }

    private sealed record SessionPayload(string AccessToken, string RefreshToken, Guid UserId, Guid TenantId, string Email, string RoleName);
}
