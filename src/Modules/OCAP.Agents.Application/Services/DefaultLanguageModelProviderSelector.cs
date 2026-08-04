using OCAP.Agents.Abstractions.Providers;
using OCAP.Intelligence.Abstractions;

namespace OCAP.Agents.Application.Services;

// Selector dinámico de proveedores de modelos de lenguaje aislado por Tenant con failover a proveedores registrados.
public class DefaultLanguageModelProviderSelector : ILanguageModelProviderSelector
{
    private readonly IEnumerable<ILanguageModelProvider> _staticProviders;
    private readonly IEnumerable<IAiProvider> _aiProviders;
    private readonly IAiProviderConfigurationService? _configurationService;

    public DefaultLanguageModelProviderSelector(
        IEnumerable<ILanguageModelProvider> staticProviders,
        IEnumerable<IAiProvider> aiProviders,
        IAiProviderConfigurationService? configurationService = null)
    {
        _staticProviders = staticProviders;
        _aiProviders = aiProviders;
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
                // Fallback silencioso a proveedores estáticos / registrados
            }
        }

        // 2. Resolver desde lista estática de ILanguageModelProvider
        ILanguageModelProvider? selected = null;
        if (!string.IsNullOrEmpty(preferredProvider))
        {
            selected = _staticProviders.FirstOrDefault(p =>
                string.Equals(p.ProviderName, preferredProvider, StringComparison.OrdinalIgnoreCase));
        }

        selected ??= _staticProviders.FirstOrDefault();

        // 3. Adaptar IAiProvider registrados (Mock, OpenAI, etc.)
        if (selected == null)
        {
            IAiProvider? ai = null;
            if (!string.IsNullOrEmpty(preferredProvider))
            {
                ai = _aiProviders.FirstOrDefault(p =>
                    string.Equals(p.Name, preferredProvider, StringComparison.OrdinalIgnoreCase));
            }

            // Preferir Gemini / OpenAI reales sobre Mock.
            ai ??= _aiProviders.FirstOrDefault(p =>
                string.Equals(p.Name, "Gemini", StringComparison.OrdinalIgnoreCase));
            ai ??= _aiProviders.FirstOrDefault(p =>
                string.Equals(p.Name, "OpenAI", StringComparison.OrdinalIgnoreCase));
            ai ??= _aiProviders.FirstOrDefault(p =>
                !string.Equals(p.Name, "Mock", StringComparison.OrdinalIgnoreCase));
            ai ??= _aiProviders.FirstOrDefault(p =>
                string.Equals(p.Name, "Mock", StringComparison.OrdinalIgnoreCase));
            ai ??= _aiProviders.FirstOrDefault();

            if (ai != null)
            {
                return new LanguageModelProviderAdapter(ai);
            }
        }

        if (selected == null)
        {
            throw new InvalidOperationException("No LanguageModelProvider is available to process the request.");
        }

        return selected;
    }
}
