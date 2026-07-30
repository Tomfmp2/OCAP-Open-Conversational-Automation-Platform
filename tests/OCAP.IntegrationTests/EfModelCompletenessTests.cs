using Microsoft.EntityFrameworkCore;
using OCAP.Infrastructure.Persistence.Context;
using FluentAssertions;

namespace OCAP.IntegrationTests;

/// <summary>
/// Valida que el modelo EF Core de OCAP esté completo y alineado con las migraciones.
/// </summary>
public class EfModelCompletenessTests
{
    private static OCAPDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<OCAPDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new OCAPDbContext(options);
    }

    [Fact]
    public void Every_DbSet_entity_is_mapped_in_the_model()
    {
        using var context = CreateInMemoryContext();
        var model = context.Model;

        var dbSetEntityTypes = typeof(OCAPDbContext)
            .GetProperties()
            .Where(p => p.PropertyType.IsGenericType
                        && p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
            .Select(p => p.PropertyType.GetGenericArguments()[0])
            .ToList();

        dbSetEntityTypes.Should().HaveCount(52);

        foreach (var clrType in dbSetEntityTypes)
        {
            var entityType = model.FindEntityType(clrType);
            entityType.Should().NotBeNull($"DbSet<{clrType.Name}> must be mapped");
            entityType!.GetTableName().Should().NotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public void Model_can_be_created_in_memory_without_errors()
    {
        using var context = CreateInMemoryContext();
        context.Database.EnsureCreated();

        var entityTypes = context.Model.GetEntityTypes()
            .Where(e => !e.IsOwned())
            .Select(e => e.ClrType.Name)
            .OrderBy(n => n)
            .ToList();

        entityTypes.Should().Contain(new[]
        {
            "User", "Conversation", "Message", "Session",
            "Tenant", "UserIdentity", "Role", "Permission",
            "WorkflowDefinition", "WorkflowExecution", "WorkflowVariable",
            "KnowledgeBase", "KnowledgeDocument", "KnowledgeChunk",
            "OutboxMessage", "Agent", "ToolExecution"
        });
    }

    [Fact]
    public void Snapshot_matches_current_model_when_using_relational_provider()
    {
        // Usa el factory de diseño (Npgsql) para comparar el modelo runtime con el snapshot.
        var factory = new OCAPDbContextFactory();
        using var context = factory.CreateDbContext([]);

        context.Database.HasPendingModelChanges().Should().BeFalse(
            "there must be no pending model changes after InitialCreate regeneration");
    }

    [Fact]
    public void ApplyConfigurationsFromAssembly_covers_all_non_openiddict_entity_types()
    {
        using var context = CreateInMemoryContext();

        var configuredTypes = context.Model.GetEntityTypes()
            .Where(e => e.ClrType.Namespace is not null
                        && !e.ClrType.Namespace.StartsWith("OpenIddict", StringComparison.Ordinal))
            .Where(e => !e.IsOwned())
            .Select(e => e.ClrType)
            .ToHashSet();

        var dbSetTypes = typeof(OCAPDbContext)
            .GetProperties()
            .Where(p => p.PropertyType.IsGenericType
                        && p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
            .Select(p => p.PropertyType.GetGenericArguments()[0])
            .ToHashSet();

        dbSetTypes.Should().BeSubsetOf(configuredTypes);
    }
}
