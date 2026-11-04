using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OCAP.Agents.Abstractions.Providers;
using OCAP.Intelligence.Abstractions;
using OCAP.Intelligence.Domain;

namespace OCAP.Agents.Application.Services;

/// <summary>
/// Si el modelo indicado no responde (cuota agotada, caída o plazo vencido), intenta una vez con otro modelo activo del
/// mismo proveedor. Los dos intentos caben en el plazo del proxy del panel (130 s).
/// </summary>
public sealed class FallbackLanguageModelProvider : ILanguageModelProvider
{
    private static readonly TimeSpan DefaultAttemptTimeout = TimeSpan.FromSeconds(60);

    private readonly IAiProvider _primary;
    private readonly Guid _tenantId;
    private readonly IAiProviderConfigurationService _configurations;
    private readonly ILogger _logger;
    private readonly TimeSpan _attemptTimeout;

    public FallbackLanguageModelProvider(
        IAiProvider primary,
        Guid tenantId,
        IAiProviderConfigurationService configurations,
        ILogger? logger = null,
        TimeSpan? attemptTimeout = null)
    {
        _primary = primary ?? throw new ArgumentNullException(nameof(primary));
        _configurations = configurations ?? throw new ArgumentNullException(nameof(configurations));
        _tenantId = tenantId;
        _logger = logger ?? NullLogger.Instance;
        _attemptTimeout = attemptTimeout ?? DefaultAttemptTimeout;
    }

    public string ProviderName => _primary.Name;

    public async Task<LanguageModelResponse> GenerateAsync(LanguageModelRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            return await AttemptAsync(_primary, request, cancellationToken);
        }
        catch (Exception ex) when (OtherModelMayWork(ex, cancellationToken))
        {
            var failed = _primary.GetModelInformation();
            var fallback = await FindFallbackAsync(failed, cancellationToken);
            if (fallback is null) throw;

            _logger.LogWarning(ex, "El modelo {Provider} {Model} falló; se intenta con {Fallback}.", failed.Provider, failed.Model, fallback.Name);
            return await AttemptAsync(fallback, request, cancellationToken);
        }
    }

    private async Task<LanguageModelResponse> AttemptAsync(IAiProvider provider, LanguageModelRequest request, CancellationToken cancellationToken)
    {
        using var attempt = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        attempt.CancelAfter(_attemptTimeout);
        return await new LanguageModelProviderAdapter(provider).GenerateAsync(request, attempt.Token);
    }

    private async Task<IAiProvider?> FindFallbackAsync(AiModelInformation failed, CancellationToken cancellationToken)
    {
        try
        {
            return await _configurations.GetFallbackProviderAsync(_tenantId, failed.Provider, failed.Model, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "No se pudo buscar otro modelo para la organización {TenantId}.", _tenantId);
            return null;
        }
    }

    private static bool OtherModelMayWork(Exception ex, CancellationToken cancellationToken) => ex switch
    {
        AiProviderException provider => provider.OtherModelMayWork,
        OperationCanceledException => !cancellationToken.IsCancellationRequested,
        HttpRequestException => true,
        _ => false,
    };
}
