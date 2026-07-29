namespace OCAP.Security.Domain.Entities;

// Entidad de configuración de conexión LDAP / Active Directory por Tenant (CAP-19).
public class LdapProviderConfig
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string Server { get; private set; } = string.Empty;
    public int Port { get; private set; } = 389;
    public bool UseSsl { get; private set; } = false;
    public string BindDn { get; private set; } = string.Empty;
    public string EncryptedBindPassword { get; private set; } = string.Empty;
    public string BaseDn { get; private set; } = string.Empty;
    public string UserSearchFilter { get; private set; } = "(objectClass=person)";
    public string GroupSearchFilter { get; private set; } = "(objectClass=group)";
    public bool IsEnabled { get; private set; } = true;
    public string AttributeMappingJson { get; private set; } = "{\"email\":\"mail\",\"fullName\":\"displayName\",\"username\":\"sAMAccountName\"}";
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    private LdapProviderConfig() { }

    public LdapProviderConfig(
        Guid id,
        Guid tenantId,
        string server,
        int port,
        bool useSsl,
        string bindDn,
        string encryptedBindPassword,
        string baseDn,
        string? userSearchFilter = null,
        string? groupSearchFilter = null,
        string? attributeMappingJson = null)
    {
        if (id == Guid.Empty) throw new ArgumentException("El ID no puede ser vacío.", nameof(id));
        if (tenantId == Guid.Empty) throw new ArgumentException("El TenantId no puede ser vacío.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(server)) throw new ArgumentException("El servidor LDAP es obligatorio.", nameof(server));
        if (string.IsNullOrWhiteSpace(baseDn)) throw new ArgumentException("El BaseDN es obligatorio.", nameof(baseDn));

        Id = id;
        TenantId = tenantId;
        Server = server.Trim();
        Port = port <= 0 ? (useSsl ? 636 : 389) : port;
        UseSsl = useSsl;
        BindDn = bindDn?.Trim() ?? string.Empty;
        EncryptedBindPassword = encryptedBindPassword ?? string.Empty;
        BaseDn = baseDn.Trim();
        UserSearchFilter = string.IsNullOrWhiteSpace(userSearchFilter) ? "(objectClass=person)" : userSearchFilter.Trim();
        GroupSearchFilter = string.IsNullOrWhiteSpace(groupSearchFilter) ? "(objectClass=group)" : groupSearchFilter.Trim();
        AttributeMappingJson = string.IsNullOrWhiteSpace(attributeMappingJson) ? "{\"email\":\"mail\",\"fullName\":\"displayName\",\"username\":\"sAMAccountName\"}" : attributeMappingJson.Trim();
        IsEnabled = true;
        CreatedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Update(string server, int port, bool useSsl, string bindDn, string encryptedBindPassword, string baseDn, string? userSearchFilter, string? groupSearchFilter, string? attributeMappingJson)
    {
        Server = server.Trim();
        Port = port <= 0 ? (useSsl ? 636 : 389) : port;
        UseSsl = useSsl;
        BindDn = bindDn?.Trim() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(encryptedBindPassword)) EncryptedBindPassword = encryptedBindPassword;
        BaseDn = baseDn.Trim();
        if (!string.IsNullOrWhiteSpace(userSearchFilter)) UserSearchFilter = userSearchFilter.Trim();
        if (!string.IsNullOrWhiteSpace(groupSearchFilter)) GroupSearchFilter = groupSearchFilter.Trim();
        if (!string.IsNullOrWhiteSpace(attributeMappingJson)) AttributeMappingJson = attributeMappingJson.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Enable() { IsEnabled = true; UpdatedAtUtc = DateTime.UtcNow; }
    public void Disable() { IsEnabled = false; UpdatedAtUtc = DateTime.UtcNow; }
}
