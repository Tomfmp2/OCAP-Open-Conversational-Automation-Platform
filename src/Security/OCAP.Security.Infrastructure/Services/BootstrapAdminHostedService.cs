using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OCAP.Infrastructure.Persistence.Context;
using OCAP.Security.Abstractions;
using OCAP.Security.Domain.Entities;

namespace OCAP.Security.Infrastructure.Services;

/// <summary>
/// Crea tenant/admin inicial solo si la base no tiene usuarios (bootstrap controlado por env).
/// </summary>
public sealed class BootstrapAdminHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BootstrapAdminHostedService> _logger;

    public BootstrapAdminHostedService(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<BootstrapAdminHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var enabled = _configuration.GetValue("Bootstrap:Enabled", true);
        if (!enabled) return;

        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OCAPDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        if (!db.Database.IsRelational())
        {
            // InMemory test hosts may seed themselves.
        }

        var hasUsers = await db.UserIdentities.IgnoreQueryFilters().AnyAsync(cancellationToken);
        if (hasUsers) return;

        var email = _configuration["Bootstrap:AdminEmail"] ?? "admin@ocap.io";
        var password = _configuration["Bootstrap:AdminPassword"] ?? "ChangeMe_Admin_2026!";
        var tenantName = _configuration["Bootstrap:TenantName"] ?? "OCAP Default";
        var tenantSlug = _configuration["Bootstrap:TenantSlug"] ?? "default";

        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var (hash, salt) = hasher.HashPassword(password);

        var tenant = new Tenant(tenantId, tenantName, tenantSlug);
        var role = new Role(roleId, tenantId, "Admin", "Administrador del tenant", new[]
        {
            "Conversation.Read", "Conversation.Write", "Conversation.Delete",
            "Agent.Read", "Agent.Write", "Agent.Execute", "Tool.Execute",
            "Dashboard.Read", "Dashboard.Admin", "Deployment.Manage", "AI.Execute",
            "Settings.Manage", "OAuth.Manage", "Knowledge.Manage", "Workflow.Manage",
            "Security.Manage", "Channel.Manage"
        });
        var user = new UserIdentity(userId, tenantId, email, hash, salt, "Administrator");
        user.VerifyEmail();
        var userRole = new UserRole(Guid.NewGuid(), userId, roleId, tenantId);

        db.Tenants.Add(tenant);
        db.Roles.Add(role);
        db.UserIdentities.Add(user);
        db.UserRoles.Add(userRole);
        await db.SaveChangesAsync(cancellationToken);

        _logger.LogWarning(
            "Bootstrap admin created for {Email}. Change Bootstrap:AdminPassword immediately in production.",
            email);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
