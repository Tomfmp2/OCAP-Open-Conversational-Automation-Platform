using Microsoft.EntityFrameworkCore;
using OCAP.Infrastructure.Persistence.Context;
using OCAP.Security.Abstractions;
using OCAP.Security.Domain.Entities;

namespace OCAP.Security.Infrastructure.Services;

// Servicio de infraestructura para resolver y vincular identidades externas (Telegram, WhatsApp, Slack, etc.) con usuarios globales de OCAP.
public class ExternalIdentityResolver : IExternalIdentityResolver
{
    private readonly OCAPDbContext _dbContext;

    public ExternalIdentityResolver(OCAPDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    // Busca el UserId interno asociado a una identidad de canal externo para un Tenant específico.
    public async Task<Guid?> ResolveUserIdAsync(
        Guid tenantId,
        string provider,
        string externalId,
        CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty || string.IsNullOrWhiteSpace(provider) || string.IsNullOrWhiteSpace(externalId))
        {
            return null;
        }

        var normalizedProvider = provider.Trim();
        var normalizedExternalId = externalId.Trim();

        var identity = await _dbContext.ExternalIdentities
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.TenantId == tenantId &&
                x.Provider == normalizedProvider &&
                x.ExternalId == normalizedExternalId,
                cancellationToken);

        return identity?.UserId;
    }

    // Vincula una identidad externa a un UserId interno de OCAP dentro de un Tenant.
    public async Task<bool> LinkExternalIdentityAsync(
        Guid tenantId,
        Guid userId,
        string provider,
        string externalId,
        Dictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty || userId == Guid.Empty ||
            string.IsNullOrWhiteSpace(provider) || string.IsNullOrWhiteSpace(externalId))
        {
            return false;
        }

        var normalizedProvider = provider.Trim();
        var normalizedExternalId = externalId.Trim();

        var existing = await _dbContext.ExternalIdentities
            .FirstOrDefaultAsync(x =>
                x.TenantId == tenantId &&
                x.Provider == normalizedProvider &&
                x.ExternalId == normalizedExternalId,
                cancellationToken);

        if (existing != null)
        {
            // Ya está vinculada esta identidad externa a este u otro usuario.
            return existing.UserId == userId;
        }

        var newIdentity = new ExternalIdentity(
            Guid.NewGuid(),
            tenantId,
            userId,
            normalizedProvider,
            normalizedExternalId,
            metadata);

        await _dbContext.ExternalIdentities.AddAsync(newIdentity, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}
