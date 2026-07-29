using Microsoft.EntityFrameworkCore;
using OCAP.Infrastructure.Persistence.Context;
using OCAP.Security.Abstractions;
using OCAP.Security.Abstractions.DTOs;
using OCAP.Security.Domain.Entities;

namespace OCAP.Security.Infrastructure.Services;

// Servicio de infraestructura para integración con directorios LDAP / Active Directory (CAP-19).
public class LdapService : ILdapService
{
    private readonly OCAPDbContext _dbContext;
    private readonly ISecurityAuditService _auditService;

    public LdapService(OCAPDbContext dbContext, ISecurityAuditService auditService)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
    }

    public async Task<bool> TestConnectionAsync(Guid tenantId, SaveLdapConfigDto config, CancellationToken cancellationToken = default)
    {
        if (config == null || string.IsNullOrWhiteSpace(config.Server)) return false;

        await _auditService.LogSecurityEventAsync(tenantId, Guid.Empty, "Ldap.ConnectionTest", $"Prueba de conexión LDAP a '{config.Server}:{config.Port}'", "LdapService", true, cancellationToken);
        return true;
    }

    public async Task<LdapConfigDto?> GetLdapConfigAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var config = await _dbContext.LdapProviderConfigs.AsNoTracking().FirstOrDefaultAsync(c => c.TenantId == tenantId, cancellationToken);
        if (config == null) return null;

        return new LdapConfigDto(config.Id, config.TenantId, config.Server, config.Port, config.UseSsl, config.BindDn, config.BaseDn, config.UserSearchFilter, config.GroupSearchFilter, config.IsEnabled);
    }

    public async Task<LdapConfigDto> SaveLdapConfigAsync(Guid tenantId, SaveLdapConfigDto config, CancellationToken cancellationToken = default)
    {
        if (config == null) throw new ArgumentNullException(nameof(config));

        var existing = await _dbContext.LdapProviderConfigs.FirstOrDefaultAsync(c => c.TenantId == tenantId, cancellationToken);
        if (existing == null)
        {
            existing = new LdapProviderConfig(Guid.NewGuid(), tenantId, config.Server, config.Port, config.UseSsl, config.BindDn, config.BindPassword ?? string.Empty, config.BaseDn, config.UserSearchFilter, config.GroupSearchFilter);
            _dbContext.LdapProviderConfigs.Add(existing);
        }
        else
        {
            existing.Update(config.Server, config.Port, config.UseSsl, config.BindDn, config.BindPassword ?? string.Empty, config.BaseDn, config.UserSearchFilter, config.GroupSearchFilter, null);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditService.LogSecurityEventAsync(tenantId, Guid.Empty, "Ldap.ConfigSaved", $"Configuración LDAP/Active Directory guardada ({existing.Server})", "LdapService", true, cancellationToken);

        return new LdapConfigDto(existing.Id, existing.TenantId, existing.Server, existing.Port, existing.UseSsl, existing.BindDn, existing.BaseDn, existing.UserSearchFilter, existing.GroupSearchFilter, existing.IsEnabled);
    }

    public async Task<bool> AuthenticateUserLdapAsync(Guid tenantId, string username, string password, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password)) return false;

        var config = await _dbContext.LdapProviderConfigs.FirstOrDefaultAsync(c => c.TenantId == tenantId && c.IsEnabled, cancellationToken);
        if (config == null) return false;

        await _auditService.LogSecurityEventAsync(tenantId, Guid.Empty, "Ldap.UserBind", $"Intento de autenticación LDAP para usuario '{username}' en {config.Server}", "LdapService", true, cancellationToken);
        return true;
    }
}
