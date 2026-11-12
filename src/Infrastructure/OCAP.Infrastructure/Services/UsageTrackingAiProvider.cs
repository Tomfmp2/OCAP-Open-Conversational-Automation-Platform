using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OCAP.Agents.Domain.Entities;
using OCAP.Intelligence.Abstractions;
using OCAP.Intelligence.Domain;
using OCAP.Security.Abstractions;

namespace OCAP.Infrastructure.Services;

/// <summary>
/// Registra cada llamada a un modelo del panel, venga de un canal, del asistente, de la memoria o de una prueba. Escribe
/// en su propio scope para no guardar de paso los cambios a medias de quien llamó, y un fallo al registrar no afecta a
/// la respuesta.
/// </summary>
public sealed class UsageTrackingAiProvider : IAiProvider
{
    private readonly IAiProvider _inner;
    private readonly Guid _tenantId;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger _logger;

    public UsageTrackingAiProvider(IAiProvider inner, Guid tenantId, IServiceScopeFactory scopeFactory, ILogger logger)
    {
        _inner = inner;
        _tenantId = tenantId;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public string Name => _inner.Name;

    public async Task<AiResponse> GenerateResponseAsync(AiRequest request, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var response = await _inner.GenerateResponseAsync(request, cancellationToken);
            await RecordAsync(response.ModelName, response.TokensUsed, stopwatch.Elapsed, success: true);
            return response;
        }
        catch (Exception)
        {
            // También las cancelaciones: el plazo de un intento llega como tal y la llamada al proveedor ya se hizo.
            await RecordAsync(_inner.GetModelInformation().Model, tokens: 0, stopwatch.Elapsed, success: false);
            throw;
        }
    }

    public async IAsyncEnumerable<string> StreamResponseAsync(AiRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        await foreach (var chunk in _inner.StreamResponseAsync(request, cancellationToken))
        {
            yield return chunk;
        }
        await RecordAsync(_inner.GetModelInformation().Model, tokens: 0, stopwatch.Elapsed, success: true);
    }

    public Task<Intent> AnalyzeIntentAsync(string message, CancellationToken cancellationToken = default) =>
        _inner.AnalyzeIntentAsync(message, cancellationToken);

    public Task<T?> ExtractStructuredDataAsync<T>(string text, CancellationToken cancellationToken = default) =>
        _inner.ExtractStructuredDataAsync<T>(text, cancellationToken);

    public AiModelInformation GetModelInformation() => _inner.GetModelInformation();

    public Task<ProviderHealth> HealthAsync(CancellationToken cancellationToken = default) => _inner.HealthAsync(cancellationToken);

    public Task<IReadOnlyList<string>> GetAvailableModelsAsync(CancellationToken cancellationToken = default) =>
        _inner.GetAvailableModelsAsync(cancellationToken);

    private async Task RecordAsync(string model, int tokens, TimeSpan elapsed, bool success)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            scope.ServiceProvider.GetRequiredService<ITenantScope>().Use(_tenantId);
            var repository = scope.ServiceProvider.GetRequiredService<IAiExecutionLogRepository>();
            // CancellationToken.None: una llamada ya hecha cuenta aunque el cliente se haya ido.
            await repository.SaveAsync(
                new AiExecutionLog(Guid.NewGuid(), Name, model, tokens, elapsed.TotalMilliseconds, success, _tenantId),
                CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo registrar el uso de IA de la organización {TenantId}.", _tenantId);
        }
    }
}
