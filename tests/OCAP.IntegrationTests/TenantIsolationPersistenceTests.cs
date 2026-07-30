using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OCAP.Core.Entities;
using OCAP.Infrastructure.Persistence.Context;
using OCAP.Infrastructure.Persistence.Interceptors;
using OCAP.Infrastructure.Persistence.Tenancy;
using OCAP.Security.Domain.Entities;

namespace OCAP.IntegrationTests;

public class TenantIsolationPersistenceTests
{
    private static (OCAPDbContext Context, FixedTenantContext Tenant) CreateContext(Guid tenantId, bool bypass = false)
    {
        var tenant = new FixedTenantContext(tenantId, bypass);
        var options = new DbContextOptionsBuilder<OCAPDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(new TenantSaveChangesInterceptor(tenant))
            .Options;

        return (new OCAPDbContext(options, tenant), tenant);
    }

    [Fact]
    public async Task TenantA_Cannot_Read_TenantB_Data()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var dbName = Guid.NewGuid().ToString();

        // Seed as system (bypass) so both tenants coexist in the same store.
        var systemTenant = new FixedTenantContext(Guid.Empty, bypassTenantFilters: true);
        var seedOptions = new DbContextOptionsBuilder<OCAPDbContext>()
            .UseInMemoryDatabase(dbName)
            .AddInterceptors(new TenantSaveChangesInterceptor(systemTenant))
            .Options;

        await using (var seed = new OCAPDbContext(seedOptions, systemTenant))
        {
            seed.Roles.Add(new Role(Guid.NewGuid(), tenantA, "AdminA", "A", ["x"]));
            seed.Roles.Add(new Role(Guid.NewGuid(), tenantB, "AdminB", "B", ["y"]));
            await seed.SaveChangesAsync();
        }

        var tenantAContext = new FixedTenantContext(tenantA);
        await using var contextA = new OCAPDbContext(
            new DbContextOptionsBuilder<OCAPDbContext>()
                .UseInMemoryDatabase(dbName)
                .AddInterceptors(new TenantSaveChangesInterceptor(tenantAContext))
                .Options,
            tenantAContext);

        var roles = await contextA.Roles.ToListAsync();
        roles.Should().HaveCount(1);
        roles.Single().TenantId.Should().Be(tenantA);
        roles.Should().NotContain(r => r.TenantId == tenantB);
    }

    [Fact]
    public async Task Insert_Automatically_Assigns_Current_TenantId()
    {
        var tenantId = Guid.NewGuid();
        var (context, _) = CreateContext(tenantId);

        var role = new Role(Guid.NewGuid(), Guid.Empty, "Ops", "Ops role", ["a"]);
        context.Roles.Add(role);
        await context.SaveChangesAsync();

        role.TenantId.Should().Be(tenantId);

        var stored = await context.Roles.SingleAsync();
        stored.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public async Task CrossTenant_Insert_Is_Rejected()
    {
        var tenantId = Guid.NewGuid();
        var otherTenant = Guid.NewGuid();
        var (context, _) = CreateContext(tenantId);

        context.Roles.Add(new Role(Guid.NewGuid(), otherTenant, "Foreign", "x", ["a"]));

        var act = async () => await context.SaveChangesAsync();
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Cross-tenant insert*");
    }

    [Fact]
    public async Task Update_Cannot_Change_TenantId()
    {
        var tenantId = Guid.NewGuid();
        var (context, _) = CreateContext(tenantId);

        var role = new Role(Guid.NewGuid(), tenantId, "Ops", "Ops", ["a"]);
        context.Roles.Add(role);
        await context.SaveChangesAsync();

        var entry = context.Entry(role);
        entry.Property(nameof(Role.TenantId)).CurrentValue = Guid.NewGuid();
        entry.Property(nameof(Role.TenantId)).IsModified = true;

        var act = async () => await context.SaveChangesAsync();
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*immutable*");
    }

    [Fact]
    public async Task Spoofed_Tenant_Header_Does_Not_Affect_Authenticated_Claim_Context()
    {
        // Persistence isolation relies on ITenantContext already resolved (claim wins in HttpTenantContext).
        // Here we prove the DbContext only exposes the resolved tenant's rows.
        var claimedTenant = Guid.NewGuid();
        var spoofedTenant = Guid.NewGuid();
        var dbName = Guid.NewGuid().ToString();

        var system = new FixedTenantContext(Guid.Empty, bypassTenantFilters: true);
        await using (var seed = new OCAPDbContext(
                         new DbContextOptionsBuilder<OCAPDbContext>()
                             .UseInMemoryDatabase(dbName)
                             .AddInterceptors(new TenantSaveChangesInterceptor(system))
                             .Options,
                         system))
        {
            seed.ApiKeys.Add(new ApiKey(Guid.NewGuid(), claimedTenant, Guid.NewGuid(), "hash", "pfx", "claimed", DateTime.UtcNow.AddDays(1)));
            seed.ApiKeys.Add(new ApiKey(Guid.NewGuid(), spoofedTenant, Guid.NewGuid(), "hash2", "pfx2", "spoofed", DateTime.UtcNow.AddDays(1)));
            await seed.SaveChangesAsync();
        }

        var claimedContext = new FixedTenantContext(claimedTenant);
        await using var db = new OCAPDbContext(
            new DbContextOptionsBuilder<OCAPDbContext>()
                .UseInMemoryDatabase(dbName)
                .AddInterceptors(new TenantSaveChangesInterceptor(claimedContext))
                .Options,
            claimedContext);

        var keys = await db.ApiKeys.ToListAsync();
        keys.Should().HaveCount(1);
        keys.Single().Name.Should().Be("claimed");
    }

    [Fact]
    public async Task System_Bypass_Can_Read_All_Tenants_Intentionally()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var (context, _) = CreateContext(Guid.Empty, bypass: true);

        context.Roles.Add(new Role(Guid.NewGuid(), tenantA, "A", "A", ["x"]));
        context.Roles.Add(new Role(Guid.NewGuid(), tenantB, "B", "B", ["y"]));
        await context.SaveChangesAsync();

        (await context.Roles.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task Conversation_User_Entities_Are_Tenant_Filtered()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var dbName = Guid.NewGuid().ToString();

        var system = new FixedTenantContext(Guid.Empty, bypassTenantFilters: true);
        await using (var seed = new OCAPDbContext(
                         new DbContextOptionsBuilder<OCAPDbContext>()
                             .UseInMemoryDatabase(dbName)
                             .AddInterceptors(new TenantSaveChangesInterceptor(system))
                             .Options,
                         system))
        {
            var userA = new User(Guid.NewGuid(), "A", tenantA);
            var userB = new User(Guid.NewGuid(), "B", tenantB);
            seed.Users.AddRange(userA, userB);
            seed.Conversations.Add(new Conversation(Guid.NewGuid(), userA.Id, tenantA));
            seed.Conversations.Add(new Conversation(Guid.NewGuid(), userB.Id, tenantB));
            await seed.SaveChangesAsync();
        }

        var tenantAContext = new FixedTenantContext(tenantA);
        await using var db = new OCAPDbContext(
            new DbContextOptionsBuilder<OCAPDbContext>()
                .UseInMemoryDatabase(dbName)
                .AddInterceptors(new TenantSaveChangesInterceptor(tenantAContext))
                .Options,
            tenantAContext);

        (await db.Users.CountAsync()).Should().Be(1);
        (await db.Conversations.CountAsync()).Should().Be(1);
        (await db.Users.SingleAsync()).DisplayName.Should().Be("A");
    }
}
