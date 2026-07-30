namespace OCAP.Security.Abstractions;

/// <summary>
/// Proporciona acceso seguro y desacoplado al contexto del Tenant activo de la petición.
/// </summary>
public interface ITenantContext
{
    /// <summary>
    /// Identificador único del Tenant actual.
    /// </summary>
    Guid TenantId { get; }

    /// <summary>
    /// Nombre o identificador legible del Tenant.
    /// </summary>
    string TenantName { get; }

    /// <summary>
    /// Indica si el contexto del Tenant fue resuelto correctamente desde la petición o credenciales.
    /// </summary>
    bool IsResolved { get; }

    /// <summary>
    /// Cuando es true, los HasQueryFilter de EF Core no restringen por TenantId
    /// (jobs de sistema, design-time, operaciones de plataforma explícitas).
    /// </summary>
    bool BypassTenantFilters { get; }
}
