using OCAP.Agents.Abstractions.Providers;
using OCAP.Intelligence.Abstractions;

namespace OCAP.Agents.Application.Services;

// Selector dinámico de proveedores de modelos de lenguaje aislado por Tenant con failover a proveedores registrados.
public class DefaultLanguageModelProviderSelector : ILanguageModelProviderSelector
{
    private readonly IEnumerable<ILanguageModelProvider> _staticProviders;
    private readonly IAiProviderConfigurationService? _configurationService;

    public DefaultLanguageModelProviderSelector(
        IEnumerable<ILanguageModelProvider> staticProviders,
        IAiProviderConfigurationService? configurationService = null)
    {
        _staticProviders = staticProviders;
        _configurationService = configurationService;
    }

    public async Task<ILanguageModelProvider> GetProviderAsync(Guid tenantId, string? preferredProvider = null, CancellationToken cancellationToken = default)
    {
        // 1. Intentar resolver mediante el servicio dinámico de configuraciones por Tenant
        if (_configurationService != null && tenantId != Guid.Empty)
        {
            try
            {
                var aiProvider = await _configurationService.GetRuntimeProviderForTenantAsync(tenantId, preferredProvider, cancellationToken);
                if (aiProvider != null)
                {
                    return new LanguageModelProviderAdapter(aiProvider);
                }
            }
            catch
            {
                // Fallback silencioso a proveedores estáticos si la consulta dinámica falla
            }
        }

        // 2. Resolver desde lista estática de proveedores registrados
        var selected = _staticProviders.FirstOrDefault();
        if (!string.IsNullOrEmpty(preferredProvider))
        {
            selected = _staticProviders.FirstOrDefault(p => string.Equals(p.ProviderName, preferredProvider, StringComparison.OrdinalIgnoreCase))
                       ?? _staticProviders.FirstOrDefault();
        }

        if (selected == null)
        {
            throw new InvalidOperationException($"No LanguageModelProvider is available to process the request.");
        }

        return selected;
    }
}
