using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OCAP.Infrastructure.Persistence.Context;
using OCAP.Intelligence.Abstractions;
using OCAP.Knowledge.Abstractions;
using OCAP.Security.Abstractions;

namespace OCAP.Api.Services;

/// <summary>
/// Embeddings con las keys que la organización registró en IA y modelos (antes solo servía una key de OpenAI en el
/// .env). Si ya indexó documentos, sigue con el mismo proveedor y modelo: la búsqueda solo funciona comparando
/// vectores del mismo modelo. Si no, usa el primero que tenga: OpenAI, Gemini u Ollama.
/// </summary>
public sealed class TenantEmbeddingTargetResolver : IEmbeddingTargetResolver
{
    private static readonly string[] Preference = ["OpenAI", "Gemini", "Ollama"];

    private readonly OCAPDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IAiProviderConfigurationService _configurations;
    private readonly KnowledgeOptions _options;

    public TenantEmbeddingTargetResolver(
        OCAPDbContext db,
        ITenantContext tenant,
        IAiProviderConfigurationService configurations,
        IOptions<KnowledgeOptions> options)
    {
        _db = db;
        _tenant = tenant;
        _configurations = configurations;
        _options = options.Value;
    }

    public async Task<EmbeddingTarget?> ResolveAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = _tenant.TenantId;
        if (tenantId == Guid.Empty) return null;

        var indexed = await _db.KnowledgeEmbeddings
            .AsNoTracking()
            .Where(e => e.TenantId == tenantId)
            .OrderByDescending(e => e.CreatedAtUtc)
            .Select(e => new { e.Provider, e.Model })
            .FirstOrDefaultAsync(cancellationToken);

        var candidates = indexed is null ? Preference : Preference.Prepend(indexed.Provider).Distinct(StringComparer.OrdinalIgnoreCase);
        foreach (var provider in candidates)
        {
            var credential = await _configurations.GetCredentialAsync(tenantId, provider, cancellationToken);
            if (credential is null) continue;

            var model = indexed is not null && indexed.Provider.Equals(provider, StringComparison.OrdinalIgnoreCase)
                ? indexed.Model
                : DefaultModel(provider);
            return new EmbeddingTarget(provider, model, new EmbeddingCredential(credential.ApiKey, credential.BaseUrl));
        }

        return null;
    }

    private string DefaultModel(string provider) => provider switch
    {
        "OpenAI" => _options.OpenAI.DefaultModel,
        "Gemini" => _options.Gemini.DefaultModel,
        _ => _options.Ollama.DefaultModel
    };
}
