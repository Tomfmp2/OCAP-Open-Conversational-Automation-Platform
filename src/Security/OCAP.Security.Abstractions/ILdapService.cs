using OCAP.Security.Abstractions.DTOs;

namespace OCAP.Security.Abstractions;

// Contrato para servicio de conexión y sincronización de directorios LDAP / Active Directory (CAP-19).
public interface ILdapService
{
    Task<bool> TestConnectionAsync(Guid tenantId, SaveLdapConfigDto config, CancellationToken cancellationToken = default);
    Task<LdapConfigDto?> GetLdapConfigAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<LdapConfigDto> SaveLdapConfigAsync(Guid tenantId, SaveLdapConfigDto config, CancellationToken cancellationToken = default);
    Task<bool> AuthenticateUserLdapAsync(Guid tenantId, string username, string password, CancellationToken cancellationToken = default);
}
