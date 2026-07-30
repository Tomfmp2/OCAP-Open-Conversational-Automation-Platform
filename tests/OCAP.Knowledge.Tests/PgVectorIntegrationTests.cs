using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OCAP.Infrastructure.Persistence.Context;
using OCAP.Knowledge.Abstractions;
using OCAP.Knowledge.Domain.ValueObjects;
using OCAP.Knowledge.Infrastructure.VectorDb;
using Pgvector.EntityFrameworkCore;
using Xunit;

namespace OCAP.Knowledge.Tests;

/// <summary>
/// Optional PgVector integration tests. Require OCAP_TEST_PG pointing to Postgres with vector extension.
/// </summary>
public class PgVectorIntegrationTests
{
    private static string? ResolveConnectionString()
        => Environment.GetEnvironmentVariable("OCAP_TEST_PG")
           ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");

    private static bool CanRun()
        => !string.IsNullOrWhiteSpace(ResolveConnectionString());

    [Fact]
    public async Task PgVectorDatabase_UpsertSearchDelete_TenantIsolation()
    {
        if (!CanRun())
            return;

        var cs = ResolveConnectionString()!;
        var options = new DbContextOptionsBuilder<OCAPDbContext>()
            .UseNpgsql(cs, o => o.UseVector())
            .Options;

        await using var db = new OCAPDbContext(options);
        await db.Database.ExecuteSqlRawAsync("CREATE EXTENSION IF NOT EXISTS vector;");
        await db.Database.MigrateAsync();

        var knowledgeOptions = Options.Create(new KnowledgeOptions
        {
            EmbeddingDimensions = 1536,
            UseInMemory = false,
            VectorStore = "PgVector"
        });

        var store = new PgVectorDatabase(db, knowledgeOptions, NullLogger<PgVectorDatabase>.Instance);

        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var kbId = Guid.NewGuid();
        var docId = Guid.NewGuid();
        var chunkA = Guid.NewGuid();
        var chunkB = Guid.NewGuid();

        float[] MakeVector(float seed)
        {
            var v = new float[1536];
            v[0] = seed;
            v[1] = 1f - seed;
            return v;
        }

        await store.UpsertVectorsAsync(tenantA,
        [
            new EmbeddingVector(chunkA, "OpenAI", "text-embedding-3-small", 1536, MakeVector(1f), docId, kbId, tenantA, "{}", ["rag"])
        ]);
        await store.UpsertVectorsAsync(tenantB,
        [
            new EmbeddingVector(chunkB, "OpenAI", "text-embedding-3-small", 1536, MakeVector(0f), docId, kbId, tenantB, "{}", ["rag"])
        ]);

        var resultsA = await store.SearchVectorsAsync(tenantA, MakeVector(1f), topK: 5, minScore: 0.0, knowledgeBaseId: kbId, tags: ["rag"]);
        var resultsBLeak = await store.SearchVectorsAsync(tenantB, MakeVector(1f), topK: 5, minScore: 0.0, knowledgeBaseId: kbId);

        Assert.Contains(resultsA, r => r.ChunkId == chunkA);
        Assert.DoesNotContain(resultsBLeak, r => r.ChunkId == chunkA);

        await store.DeleteVectorsAsync(tenantA, [chunkA]);
        await store.DeleteVectorsAsync(tenantB, [chunkB]);
    }
}
