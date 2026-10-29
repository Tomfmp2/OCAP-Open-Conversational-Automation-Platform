using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OCAP.Channels.Abstractions.Contracts;
using OCAP.Channels.Abstractions.Registry;
using OCAP.Infrastructure.Persistence.Context;
using OCAP.Security.Abstractions;
using OCAP.Security.Domain.Entities;

namespace OCAP.Infrastructure.Services;

// Servicio de infraestructura para la gestión en tiempo de ejecución de conexiones de canales por Tenant.
public class ChannelConnectionManager : IChannelConnectionManager
{
    private static readonly HashSet<string> SinglePerTenantProviders = new(StringComparer.OrdinalIgnoreCase) { "WhatsApp" };

    private readonly OCAPDbContext _dbContext;
    private readonly ICredentialVault _credentialVault;
    private readonly IChannelRegistry _channelRegistry;
    private readonly ILogger<ChannelConnectionManager> _logger;

    public ChannelConnectionManager(
        OCAPDbContext dbContext,
        ICredentialVault credentialVault,
        IChannelRegistry channelRegistry,
        ILogger<ChannelConnectionManager> logger)
    {
        _dbContext = dbContext;
        _credentialVault = credentialVault;
        _channelRegistry = channelRegistry;
        _logger = logger;
    }

    public async Task<ChannelConnection> CreateConnectionAsync(
        Guid tenantId,
        string provider,
        string displayName,
        string rawSecretCredentials,
        Dictionary<string, string>? configurationMetadata = null,
        CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("El TenantId es obligatorio.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(provider)) throw new ArgumentException("El proveedor es obligatorio.", nameof(provider));
        if (string.IsNullOrWhiteSpace(displayName)) throw new ArgumentException("El nombre visible es obligatorio.", nameof(displayName));
        if (string.IsNullOrWhiteSpace(rawSecretCredentials)) throw new ArgumentException("Las credenciales son obligatorias.", nameof(rawSecretCredentials));

        var normalizedProvider = provider.Trim();

        var providerInfo = _channelRegistry.ResolveProvider(normalizedProvider);
        if (providerInfo == null)
        {
            throw new InvalidOperationException($"El proveedor de canal '{normalizedProvider}' no está registrado en el catálogo de OCAP.");
        }

        if (!providerInfo.IsImplemented)
        {
            throw new InvalidOperationException(
                $"El proveedor '{normalizedProvider}' está en el catálogo pero aún no tiene adaptador runtime (próximamente).");
        }

        // Varios canales del mismo tipo por organización (varios bots de Telegram, varios chats web). WhatsApp por QR
        // es una sola instancia de Evolution por organización.
        if (SinglePerTenantProviders.Contains(normalizedProvider)
            && await _dbContext.ChannelConnections.AnyAsync(c => c.TenantId == tenantId && c.Provider == normalizedProvider, cancellationToken))
        {
            throw new InvalidOperationException($"Ya hay un canal {normalizedProvider} en esta organización.");
        }

        var secretRef = await _credentialVault.StoreSecretAsync(tenantId, $"{normalizedProvider}_Credentials", rawSecretCredentials, cancellationToken);

        var connection = new ChannelConnection(
            Guid.NewGuid(),
            tenantId,
            normalizedProvider,
            displayName,
            secretRef,
            configurationMetadata,
            enabled: true);

        await _dbContext.ChannelConnections.AddAsync(connection, cancellationToken);
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Índice único de la base (p. ej. el mismo bot de Telegram en otra conexión u organización).
            _dbContext.Entry(connection).State = EntityState.Detached;
            throw new InvalidOperationException("Ese canal ya está conectado en OCAP.");
        }

        _logger.LogInformation("Nueva conexión de canal {Provider} (ID: {ConnectionId}) creada para Tenant {TenantId}.",
            normalizedProvider, connection.Id, tenantId);

        return connection;
    }

    public async Task<ChannelConnection?> UpdateConfigurationAsync(
        Guid tenantId,
        Guid connectionId,
        string displayName,
        string rawSecretCredentials,
        Dictionary<string, string>? configurationMetadata = null,
        CancellationToken cancellationToken = default)
    {
        var connection = await _dbContext.ChannelConnections
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Id == connectionId, cancellationToken);

        if (connection == null)
        {
            return null;
        }

        var secretRef = await _credentialVault.StoreSecretAsync(tenantId, $"{connection.Provider}_Credentials", rawSecretCredentials, cancellationToken);
        connection.UpdateConfiguration(displayName, secretRef, configurationMetadata);

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Conexión de canal {ConnectionId} actualizada para Tenant {TenantId}.", connectionId, tenantId);
        return connection;
    }

    public async Task<bool> EnableChannelAsync(Guid tenantId, Guid connectionId, CancellationToken cancellationToken = default)
    {
        var connection = await _dbContext.ChannelConnections
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Id == connectionId, cancellationToken);

        if (connection == null) return false;

        connection.Enable();
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Conexión de canal {ConnectionId} habilitada para Tenant {TenantId}.", connectionId, tenantId);
        return true;
    }

    public async Task<bool> DisableChannelAsync(Guid tenantId, Guid connectionId, CancellationToken cancellationToken = default)
    {
        var connection = await _dbContext.ChannelConnections
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Id == connectionId, cancellationToken);

        if (connection == null) return false;

        connection.Disable();
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Conexión de canal {ConnectionId} deshabilitada para Tenant {TenantId}.", connectionId, tenantId);
        return true;
    }

    public async Task<bool> RemoveConnectionAsync(Guid tenantId, Guid connectionId, CancellationToken cancellationToken = default)
    {
        var connection = await _dbContext.ChannelConnections
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Id == connectionId, cancellationToken);

        if (connection == null) return false;

        await _credentialVault.DeleteSecretAsync(tenantId, connection.CredentialsReference, cancellationToken);
        _dbContext.ChannelConnections.Remove(connection);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Conexión de canal {ConnectionId} eliminada para Tenant {TenantId}.", connectionId, tenantId);
        return true;
    }

    public async Task<IEnumerable<ChannelConnection>> GetTenantConnectionsAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ChannelConnections
            .AsNoTracking()
            .Where(c => c.TenantId == tenantId)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> RenameAsync(Guid tenantId, Guid connectionId, string displayName, CancellationToken cancellationToken = default)
    {
        var connection = await _dbContext.ChannelConnections
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Id == connectionId, cancellationToken);
        if (connection == null) return false;

        // El nombre lo eligió el usuario: los canales que se renombran solos (WhatsApp) lo respetan.
        var metadata = new Dictionary<string, string>(connection.ConfigurationMetadata) { ["CustomDisplayName"] = "true" };
        connection.UpdateConfiguration(displayName, connection.CredentialsReference, metadata);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> SetMetadataValueAsync(Guid tenantId, Guid connectionId, string key, string? value, CancellationToken cancellationToken = default)
    {
        var connection = await _dbContext.ChannelConnections
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Id == connectionId, cancellationToken);
        if (connection == null) return false;

        var metadata = new Dictionary<string, string>(connection.ConfigurationMetadata);
        if (value is null) metadata.Remove(key);
        else metadata[key] = value;

        connection.UpdateConfiguration(connection.DisplayName, connection.CredentialsReference, metadata);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public Task<string?> GetCredentialsAsync(ChannelConnection connection, CancellationToken cancellationToken = default) =>
        _credentialVault.RetrieveSecretAsync(connection.TenantId, connection.CredentialsReference, cancellationToken);

    public async Task<IReadOnlyList<ChannelConnection>> GetEnabledConnectionsByProviderAsync(string provider, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ChannelConnections
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(c => c.Provider == provider && c.Enabled)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ChannelConnection>> GetAllConnectionsByProviderAsync(string provider, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ChannelConnections
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(c => c.Provider == provider)
            .ToListAsync(cancellationToken);
    }

    public async Task<ChannelConnection?> FindConnectionAsync(Guid connectionId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ChannelConnections
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == connectionId, cancellationToken);
    }

    public async Task<ChannelHealthResult> ValidateConnectionAsync(Guid tenantId, Guid connectionId, CancellationToken cancellationToken = default)
    {
        var connection = await _dbContext.ChannelConnections
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Id == connectionId, cancellationToken);

        if (connection == null)
        {
            return new ChannelHealthResult
            {
                IsHealthy = false,
                StatusMessage = "Conexión de canal no encontrada para este Tenant.",
                CheckedAtUtc = DateTime.UtcNow
            };
        }

        return new ChannelHealthResult
        {
            IsHealthy = connection.Enabled,
            StatusMessage = connection.Enabled ? "Conexión de canal activa y disponible." : "La conexión de canal se encuentra deshabilitada.",
            CheckedAtUtc = DateTime.UtcNow,
            HealthDetails = new Dictionary<string, string>
            {
                ["Provider"] = connection.Provider,
                ["DisplayName"] = connection.DisplayName,
                ["Enabled"] = connection.Enabled.ToString()
            }
        };
    }
}
