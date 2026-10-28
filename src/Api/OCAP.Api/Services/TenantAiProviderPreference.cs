using Microsoft.EntityFrameworkCore;
using OCAP.Infrastructure.Persistence.Context;
using OCAP.Intelligence.Abstractions;
using OCAP.Security.Abstractions;

namespace OCAP.Api.Services;

/// <summary>
/// Proveedor de IA activo de la organización de la petición. Vive en la base (Tenants.PreferredAiProvider) y no en
/// el selector singleton: así una organización no cambia el proveedor de las demás y el valor sobrevive reinicios.
/// </summary>
public sealed class TenantAiProviderPreference
{
    private readonly OCAPDbContext _dbContext;
    private readonly ITenantContext _tenantContext;
    private readonly IAiProviderSelector _selector;

    public TenantAiProviderPreference(OCAPDbContext dbContext, ITenantContext tenantContext, IAiProviderSelector selector)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
        _selector = selector;
    }

    public async Task<string> GetActiveProviderNameAsync(CancellationToken cancellationToken)
    {
        var preferred = await _dbContext.Tenants
            .AsNoTracking()
            .Where(t => t.Id == _tenantContext.TenantId)
            .Select(t => t.PreferredAiProvider)
            .FirstOrDefaultAsync(cancellationToken);

        return string.IsNullOrWhiteSpace(preferred) ? _selector.ActiveProviderName : preferred;
    }

    /// <returns>false si la petición no tiene organización.</returns>
    public async Task<bool> SetActiveProviderNameAsync(string providerName, CancellationToken cancellationToken)
    {
        var tenant = await _dbContext.Tenants.FirstOrDefaultAsync(t => t.Id == _tenantContext.TenantId, cancellationToken);
        if (tenant is null) return false;

        tenant.SetPreferredAiProvider(providerName);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
