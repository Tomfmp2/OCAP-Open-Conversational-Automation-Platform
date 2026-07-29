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
                ["InMemoryDbName"] = $"OCAP_Test_{Guid.NewGuid()}"
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
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        // Seed Google Connection for IntegrationsController
        var googleConn = new OAuthConnection(
            Guid.NewGuid(),
            userId,
            "Google",
            "mock-access-token",
            "mock-refresh-token",
            DateTime.UtcNow.AddHours(1),
            "Calendar.Create,Gmail.Send,Sheets.Append"
        );
        db.OAuthConnections.Add(googleConn);

        // Seed WorkflowExecution for DashboardController metrics
        var workflowDefId = Guid.NewGuid();
        var execution = new WorkflowExecution(Guid.NewGuid(), workflowDefId, tenantId, userId);
        
        // Use reflection to set StartedAtUtc to have a realistic duration before calling Complete
        typeof(WorkflowExecution).GetProperty("StartedAtUtc")?.SetValue(execution, DateTime.UtcNow.AddMinutes(-5));
        
        execution.Complete("{}");

        db.WorkflowExecutions.Add(execution);

        db.SaveChanges();
    }
}
