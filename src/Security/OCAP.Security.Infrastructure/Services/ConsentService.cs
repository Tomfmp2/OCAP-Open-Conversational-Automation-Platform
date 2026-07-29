using Microsoft.EntityFrameworkCore;
using OCAP.Infrastructure.Persistence.Context;
using OCAP.Security.Abstractions;
using OCAP.Security.Domain.Entities;

namespace OCAP.Security.Infrastructure.Services;

// Servicio de infraestructura para persistencia y gestión de consentimiento de usuarios OAuth2/OIDC (CAP-14).
public class ConsentService : IConsentService
{
    private readonly OCAPDbContext _dbContext;
    private readonly ISecurityAuditService _auditService;

    public ConsentService(OCAPDbContext dbContext, ISecurityAuditService auditService)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
    }

    public async Task<UserConsent> GrantConsentAsync(Guid tenantId, Guid userId, string clientId, IEnumerable<string> scopes, CancellationToken cancellationToken = default)
    {
        var scopeString = string.Join(" ", scopes ?? Array.Empty<string>());
        var existing = await _dbContext.UserConsents.FirstOrDefaultAsync(c =>
            c.TenantId == tenantId && c.UserId == userId && c.ClientId == clientId && !c.IsRevoked, cancellationToken);

        if (existing != null)
        {
            _dbContext.UserConsents.Remove(existing);
        }

        var consent = new UserConsent(Guid.NewGuid(), tenantId, userId, clientId, scopeString, DateTime.UtcNow.AddYears(1));
        _dbContext.UserConsents.Add(consent);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditService.LogSecurityEventAsync(
            tenantId, userId, "Authorization.ConsentGranted", $"Consentimiento otorgado al cliente '{clientId}' para los scopes '{scopeString}'", "ConsentService", true, cancellationToken);

        return consent;
    }

    public async Task<UserConsent?> GetConsentAsync(Guid tenantId, Guid userId, string clientId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserConsents.FirstOrDefaultAsync(c =>
            c.TenantId == tenantId && c.UserId == userId && c.ClientId == clientId && !c.IsRevoked &&
            (!c.ExpiresAtUtc.HasValue || c.ExpiresAtUtc.Value > DateTime.UtcNow), cancellationToken);
    }

    public async Task<bool> HasConsentAsync(Guid tenantId, Guid userId, string clientId, IEnumerable<string> scopes, CancellationToken cancellationToken = default)
    {
        var consent = await GetConsentAsync(tenantId, userId, clientId, cancellationToken);
        if (consent == null) return false;

        if (string.IsNullOrWhiteSpace(consent.GrantedScopes) || consent.GrantedScopes == "*") return true;

        var grantedSet = new HashSet<string>(consent.GrantedScopes.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        foreach (var requiredScope in scopes ?? Array.Empty<string>())
        {
            if (!grantedSet.Contains(requiredScope)) return false;
        }

        return true;
    }

    public async Task<bool> RevokeConsentAsync(Guid consentId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        var consent = await _dbContext.UserConsents.FirstOrDefaultAsync(c => c.Id == consentId && c.TenantId == tenantId, cancellationToken);
        if (consent == null || consent.IsRevoked) return false;

        consent.Revoke();
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditService.LogSecurityEventAsync(
            tenantId, consent.UserId, "Authorization.ConsentRevoked", $"Consentimiento {consentId} revocado para cliente '{consent.ClientId}'", "ConsentService", true, cancellationToken);

        return true;
    }
}
