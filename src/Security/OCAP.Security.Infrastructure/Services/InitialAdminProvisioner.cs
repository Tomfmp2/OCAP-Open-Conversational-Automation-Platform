using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OCAP.Infrastructure.Persistence.Context;
using OCAP.Security.Abstractions;
using OCAP.Security.Domain.Entities;

namespace OCAP.Security.Infrastructure.Services;

/// <summary>Datos de la organización y el primer administrador de una instalación vacía.</summary>
public sealed record InitialAdminRequest(
    string Email,
    string Password,
    string DisplayName,
    string OrganizationName,
    string OrganizationSlug);

/// <summary>
/// Crea el primer tenant, su rol Admin y el administrador. Solo actúa sobre una base sin usuarios:
/// es el único camino para dar de alta el primer acceso (configuración inicial o bootstrap por entorno).
/// </summary>
public sealed class InitialAdminProvisioner
{
    /// <summary>Tenant fijo: coincide con el que usa HttpTenantContext para peticiones anónimas en Development.</summary>
    public static readonly Guid DefaultTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    private static readonly string[] AdminPermissions =
    {
        "Conversation.Read", "Conversation.Write", "Conversation.Delete",
        "Agent.Read", "Agent.Write", "Agent.Execute", "Tool.Execute",
        "Dashboard.Read", "Dashboard.Admin", "Deployment.Manage", "AI.Execute",
        "Settings.Manage", "OAuth.Manage", "Knowledge.Manage", "Workflow.Manage",
        "Security.Manage", "Channel.Manage"
    };

    private readonly OCAPDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IConfiguration _configuration;

    public InitialAdminProvisioner(OCAPDbContext db, IPasswordHasher hasher, IConfiguration configuration)
    {
        _db = db;
        _hasher = hasher;
        _configuration = configuration;
    }

    public Task<bool> HasUsersAsync(CancellationToken cancellationToken) =>
        _db.UserIdentities.IgnoreQueryFilters().AnyAsync(cancellationToken);

    /// <summary>Crea la organización y el admin. Falla si ya existe algún usuario: nunca modifica cuentas existentes.</summary>
    public async Task<UserIdentity> CreateAsync(InitialAdminRequest request, CancellationToken cancellationToken)
    {
        if (await HasUsersAsync(cancellationToken))
            throw new InvalidOperationException("La instalación ya tiene usuarios; el administrador inicial no se puede volver a crear.");

        var tenantId = Guid.TryParse(_configuration["Bootstrap:TenantId"], out var configured) && configured != Guid.Empty
            ? configured
            : DefaultTenantId;

        var tenant = await _db.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);
        if (tenant is null)
        {
            tenant = new Tenant(tenantId, request.OrganizationName.Trim(), request.OrganizationSlug.Trim().ToLowerInvariant());
            _db.Tenants.Add(tenant);
        }

        var (hash, salt) = _hasher.HashPassword(request.Password);
        var role = new Role(Guid.NewGuid(), tenant.Id, "Admin", "Administrador del tenant", AdminPermissions);
        var user = new UserIdentity(Guid.NewGuid(), tenant.Id, request.Email.Trim().ToLowerInvariant(), hash, salt, request.DisplayName.Trim());
        user.VerifyEmail();

        _db.Roles.Add(role);
        _db.UserIdentities.Add(user);
        _db.UserRoles.Add(new UserRole(Guid.NewGuid(), user.Id, role.Id, tenant.Id));
        await _db.SaveChangesAsync(cancellationToken);
        return user;
    }
}
