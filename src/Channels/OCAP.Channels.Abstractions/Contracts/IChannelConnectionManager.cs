using OCAP.Security.Domain.Entities;

namespace OCAP.Channels.Abstractions.Contracts;

// Contrato para la orquestación en tiempo de ejecución del ciclo de vida de conexiones de canales por Tenant.
public interface IChannelConnectionManager
{
    // Crea y almacena una nueva conexión de canal cifrando sus credenciales en el Vault.
    Task<ChannelConnection> CreateConnectionAsync(
        Guid tenantId,
        string provider,
        string displayName,
        string rawSecretCredentials,
        Dictionary<string, string>? configurationMetadata = null,
        CancellationToken cancellationToken = default);

    // Actualiza la configuración o credenciales de una conexión de canal existente.
    Task<ChannelConnection?> UpdateConfigurationAsync(
        Guid tenantId,
        Guid connectionId,
        string displayName,
        string rawSecretCredentials,
        Dictionary<string, string>? configurationMetadata = null,
        CancellationToken cancellationToken = default);

    // Activa la conexión de canal para habilitar la recepción/envío de mensajes.
    Task<bool> EnableChannelAsync(Guid tenantId, Guid connectionId, CancellationToken cancellationToken = default);

    // Desactiva la conexión de canal suspendiendo el tráfico entrante/saliente.
    Task<bool> DisableChannelAsync(Guid tenantId, Guid connectionId, CancellationToken cancellationToken = default);

    // Elimina permanentemente la conexión de canal y sus credenciales asociadas en el Vault.
    Task<bool> RemoveConnectionAsync(Guid tenantId, Guid connectionId, CancellationToken cancellationToken = default);

    // Obtiene todas las conexiones de canales registradas para un Tenant específico.
    Task<IEnumerable<ChannelConnection>> GetTenantConnectionsAsync(Guid tenantId, CancellationToken cancellationToken = default);

    // Valida la salud y disponibilidad técnica de una conexión de canal activa.
    Task<ChannelHealthResult> ValidateConnectionAsync(Guid tenantId, Guid connectionId, CancellationToken cancellationToken = default);
}
