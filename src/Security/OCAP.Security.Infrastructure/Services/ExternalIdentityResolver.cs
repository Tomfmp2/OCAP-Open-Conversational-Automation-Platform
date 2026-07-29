using Microsoft.EntityFrameworkCore;
using OCAP.Infrastructure.Persistence.Context;
using OCAP.Security.Abstractions;
using OCAP.Security.Domain.Entities;

namespace OCAP.Security.Infrastructure.Services;

// Servicio de infraestructura para resolver, vincular y desvincular identidades externas (Google, Microsoft, GitHub, OIDC, etc.) con usuarios globales de OCAP (CAP-15).
public class ExternalIdentityResolver : IExternalIdentityResolver
{
    private readonly OCAPDbContext _dbContext;

    public ExternalIdentityResolver(OCAPDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

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

        var normalizedProvider = provider.Trim().ToLowerInvariant();
        var normalizedExternalId = externalId.Trim();

        var identity = await _dbContext.ExternalIdentities
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.TenantId == tenantId &&
                x.Provider.ToLower() == normalizedProvider &&
                x.ExternalId == normalizedExternalId,
                cancellationToken);

        return identity?.UserId;
    }

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
                x.Provider.ToLower() == normalizedProvider.ToLower() &&
                x.ExternalId == normalizedExternalId,
                cancellationToken);

        if (existing != null)
        {
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

    public async Task<bool> UnlinkExternalIdentityAsync(
        Guid tenantId,
        Guid userId,
        string provider,
        CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty || userId == Guid.Empty || string.IsNullOrWhiteSpace(provider))
        {
            return false;
        }

        var normalizedProvider = provider.Trim().ToLowerInvariant();

        var existing = await _dbContext.ExternalIdentities
            .FirstOrDefaultAsync(x =>
                x.TenantId == tenantId &&
                x.UserId == userId &&
                x.Provider.ToLower() == normalizedProvider,
                cancellationToken);

        if (existing == null)
        {
            return false;
        }

        _dbContext.ExternalIdentities.Remove(existing);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<IReadOnlyList<ExternalIdentity>> GetLinkedIdentitiesAsync(
        Guid tenantId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty || userId == Guid.Empty)
        {
            return Array.Empty<ExternalIdentity>();
        }

        return await _dbContext.ExternalIdentities
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.UserId == userId)
            .ToListAsync(cancellationToken);
    }
}
