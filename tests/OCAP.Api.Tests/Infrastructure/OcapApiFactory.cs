using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OCAP.Infrastructure.Persistence.Context;
using OCAP.Core.Entities;
using OCAP.Workflow.Domain.Entities;

namespace OCAP.Api.Tests.Infrastructure;

public class OcapApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["UseInMemory"] = "true",
                ["InMemoryDbName"] = $"OCAP_Test_{Guid.NewGuid()}",
                ["Jwt:SecretKey"] = "OCAP_TESTING_JWT_SECRET_KEY_32CHARS_MINIMUM!",
                ["Jwt:Issuer"] = "OCAP",
                ["Jwt:Audience"] = "OCAP.Clients",
                ["Jwt:AccessTokenExpiryMinutes"] = "60",
                ["Security:Vault:MasterKey"] = "OCAP_TESTING_VAULT_MASTER_KEY_32CHARS_MIN!"
            });
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        using (var scope = host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<OCAPDbContext>();
            db.Database.EnsureCreated();
            SeedDatabase(db);
        }

        return host;
    }

    private void SeedDatabase(OCAPDbContext db)
    {
        // Must match HttpTenantContext default for anonymous Testing requests without X-Tenant-ID.
        var tenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var userId = Guid.NewGuid();

        // Seed Google Connection for IntegrationsController
        var googleConn = new OAuthConnection(
            Guid.NewGuid(),
            userId,
            "Google",
            "mock-access-token",
            "mock-refresh-token",
            DateTime.UtcNow.AddHours(1),
            "Calendar.Create,Gmail.Send,Sheets.Append",
            tenantId);
        db.OAuthConnections.Add(googleConn);

        // Seed WorkflowExecution for DashboardController metrics
        var workflowDefId = Guid.NewGuid();
        var definition = new WorkflowDefinition(workflowDefId, tenantId, "Dashboard Seed Workflow");
        db.WorkflowDefinitions.Add(definition);

        var execution = new WorkflowExecution(Guid.NewGuid(), workflowDefId, tenantId, userId);
        
        // Use reflection to set StartedAtUtc to have a realistic duration before calling Complete
        typeof(WorkflowExecution).GetProperty("StartedAtUtc")?.SetValue(execution, DateTime.UtcNow.AddMinutes(-5));
        
        execution.Complete("{}");

        db.WorkflowExecutions.Add(execution);

        db.SaveChanges();
    }
}
