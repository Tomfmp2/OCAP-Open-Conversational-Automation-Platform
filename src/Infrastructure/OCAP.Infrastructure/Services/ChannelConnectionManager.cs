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

        var existing = await _dbContext.ChannelConnections
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Provider == normalizedProvider, cancellationToken);

        if (existing != null)
        {
            throw new InvalidOperationException($"Ya existe una conexión registrada para el proveedor '{normalizedProvider}' en este Tenant.");
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
        await _dbContext.SaveChangesAsync(cancellationToken);

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
