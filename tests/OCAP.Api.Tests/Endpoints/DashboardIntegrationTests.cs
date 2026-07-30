using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using OCAP.Api.Controllers;
using OCAP.Api.DTOs.Requests;
using OCAP.Api.DTOs.Responses;
using OCAP.Api.Models.Dashboard;
using OCAP.Api.Models.Workflow;
using OCAP.Channels.Abstractions.Contracts;
using OCAP.Channels.Abstractions.Registry;
using OCAP.Infrastructure.Persistence.Context;
using OCAP.Security.Abstractions;
using OCAP.Security.Application.UseCases;
using OCAP.Security.Domain.Entities;
using OCAP.Workflow.Abstractions;
using OCAP.Workflow.Application.Services;
using OCAP.Workflow.Designer.DTOs;
using OCAP.Workflow.Designer.Models;
using Xunit;

namespace OCAP.Api.Tests.Endpoints;

public class DashboardIntegrationTests
{
    private static OCAPDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<OCAPDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new OCAPDbContext(options);
    }

    [Fact]
    public async Task DashboardController_GetOverviewAndDiagnostics_ReturnValidData()
    {
        // Arrange
        using var db = CreateDbContext();
        var controller = new DashboardController(db);

        // Act - Overview
        var overviewResult = await controller.GetOverview(CancellationToken.None);

        // Assert - Overview
        overviewResult.Result.Should().BeOfType<OkObjectResult>();
        var okOverview = (OkObjectResult)overviewResult.Result!;
        var overview = okOverview.Value.Should().BeOfType<DashboardOverviewDto>().Subject;

        overview.Health.Should().Be("Healthy");
        overview.Uptime.Should().NotBeNull();
        overview.Workflows.Should().NotBeNull();
        overview.Agents.Should().NotBeNull();
        overview.Channels.Should().NotBeNull();
        overview.Tenants.Should().NotBeNull();
        overview.Users.Should().NotBeNull();
        overview.ApiKeys.Should().NotBeNull();
        overview.Webhooks.Should().NotBeNull();

        // Act - Diagnostics
        var diagnosticsResult = controller.GetSignalRDiagnostics();

        // Assert - Diagnostics
        diagnosticsResult.Result.Should().BeOfType<OkObjectResult>();
        var okDiagnostics = (OkObjectResult)diagnosticsResult.Result!;
        var diagnostics = okDiagnostics.Value.Should().BeOfType<SignalRDiagnosticsDto>().Subject;

        diagnostics.HubName.Should().Be("EventsHub");
        diagnostics.EndpointUri.Should().Be("/hubs/events");
        diagnostics.Status.Should().Be("Operational");
        diagnostics.StreamedEvents.Should().Contain("WorkflowStarted");
    }

    [Fact]
    public async Task WorkflowsController_ExposesManagementAndStatusEndpoints()
    {
        // Arrange
        var engineMock = new Mock<IWorkflowEngine>();
        var validatorMock = new Mock<IWorkflowValidator>();
        var mapperMock = new Mock<IWorkflowDesignerMapper>();
        var tenantContextMock = new Mock<OCAP.Security.Abstractions.ITenantContext>();
        var userContextMock = new Mock<OCAP.Security.Abstractions.IUserContext>();
        using var dbContext = CreateDbContext();

        var controller = new WorkflowsController(engineMock.Object, validatorMock.Object, mapperMock.Object, dbContext, tenantContextMock.Object, userContextMock.Object);
        var workflowId = Guid.NewGuid();
        
        var definition = new OCAP.Workflow.Domain.Entities.WorkflowDefinition(workflowId, Guid.NewGuid(), "Test");
        dbContext.WorkflowDefinitions.Add(definition);
        await dbContext.SaveChangesAsync();

        // Act - GetById
        var getByIdResult = await controller.GetWorkflowById(workflowId, CancellationToken.None);
        getByIdResult.Result.Should().BeOfType<OkObjectResult>();

        // Act - Update (PUT)
        var updateResult = await controller.UpdateWorkflow(workflowId, new CreateWorkflowRequestDto { Name = "Test Updated", Description = "Desc Updated" }, CancellationToken.None);
        updateResult.Result.Should().BeOfType<OkObjectResult>();

        // Act - Status
        var statusResult = await controller.GetWorkflowStatus(workflowId, CancellationToken.None);
        statusResult.Result.Should().BeOfType<OkObjectResult>();

        // Act - Executions
        var executionsResult = await controller.GetExecutionsForWorkflow(workflowId, CancellationToken.None);
        executionsResult.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task AgentsController_ExposesAgentListDetailsAndRuntimeStatus()
    {
        // Arrange
        var repoMock = new Mock<OCAP.Agents.Abstractions.Ports.IAgentRepository>();
        // Mock data to prevent nulls
        repoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<OCAP.Agents.Domain.Entities.Agent>());
        var agent = new OCAP.Agents.Domain.Entities.Agent(Guid.Parse("11111111-1111-1111-1111-111111111111"), new OCAP.Agents.Domain.ValueObjects.AgentName("Test"), "Desc", new OCAP.Agents.Domain.ValueObjects.AgentConfiguration("sys", null, new List<string>()));
        repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(agent);

        var agentService = new OCAP.Agents.Application.Services.AgentService(repoMock.Object);
        var controller = new AgentsController(agentService);
        var agentId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        // Act - List
        var listResult = await controller.GetAgents(CancellationToken.None);
        listResult.Result.Should().BeOfType<OkObjectResult>();

        // Act - Details
        var detailsResult = await controller.GetAgentById(agentId, CancellationToken.None);
        detailsResult.Result.Should().BeOfType<OkObjectResult>();

        // Act - Runtime Status
        var statusResult = await controller.GetAgentRuntimeStatus(agentId, CancellationToken.None);
        statusResult.Result.Should().BeOfType<OkObjectResult>();
        var okStatus = (OkObjectResult)statusResult.Result!;
        var statusDto = okStatus.Value.Should().BeOfType<AgentRuntimeStatusDto>().Subject;
        statusDto.Status.Should().Be("Active");
    }

    [Fact]
    public async Task ChannelManagementController_ExposesProviderStatusHealthConfigurationAndStatistics()
    {
        // Arrange
        var registryMock = new Mock<IChannelRegistry>();
        var connManagerMock = new Mock<IChannelConnectionManager>();
        var tenantMock = new Mock<ITenantContext>();
        var userMock = new Mock<IUserContext>();
        var permMock = new Mock<IPermissionValidator>();
        var auditMock = new Mock<ISecurityAuditService>();

        var tenantId = Guid.NewGuid();
        tenantMock.Setup(t => t.TenantId).Returns(tenantId);
        connManagerMock.Setup(c => c.GetTenantConnectionsAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<ChannelConnection>());

        var controller = new ChannelManagementController(
            registryMock.Object, connManagerMock.Object, tenantMock.Object, userMock.Object, permMock.Object, auditMock.Object);

        // Act - Telegram Status & Health
        var telegramStatus = (await controller.GetProviderStatus("Telegram", CancellationToken.None)) as OkObjectResult;
        telegramStatus.Should().NotBeNull();
        telegramStatus!.StatusCode.Should().Be(200);

        var telegramHealth = (await controller.GetProviderHealth("Telegram", CancellationToken.None)) as OkObjectResult;
        telegramHealth.Should().NotBeNull();

        // Act - WhatsApp Config & Statistics
        var waConfig = (await controller.GetProviderConfiguration("WhatsApp", CancellationToken.None)) as OkObjectResult;
        waConfig.Should().NotBeNull();

        var waStats = (await controller.GetProviderStatistics("WhatsApp", CancellationToken.None)) as OkObjectResult;
        waStats.Should().NotBeNull();
    }

    [Fact]
    public async Task WebhooksController_ManagesSubscriptionsAndHistory()
    {
        // Arrange
        var webhookServiceMock = new Mock<IWebhookService>();
        var tenantContextMock = new Mock<ITenantContext>();
        var auditMock = new Mock<ISecurityAuditService>();

        var tenantId = Guid.NewGuid();
        tenantContextMock.Setup(t => t.TenantId).Returns(tenantId);

        var subId = Guid.NewGuid();
        var sub = new WebhookSubscription(subId, tenantId, "Test Hook", "https://example.com/webhook", "secret", "WorkflowStarted");
        webhookServiceMock.Setup(s => s.GetSubscriptionsForTenantAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WebhookSubscription> { sub });

        webhookServiceMock.Setup(s => s.CreateSubscriptionAsync(tenantId, "Test Hook", "https://example.com/webhook", It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sub);

        webhookServiceMock.Setup(s => s.DeleteSubscriptionAsync(subId, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var controller = new WebhooksController(webhookServiceMock.Object, tenantContextMock.Object, auditMock.Object);

        // Act - List
        var listResult = await controller.GetWebhooks(CancellationToken.None);
        listResult.Should().BeOfType<OkObjectResult>();

        // Act - Create
        var createResult = await controller.CreateWebhook(new CreateWebhookRequestDto
        {
            Name = "Test Hook",
            TargetUrl = "https://example.com/webhook",
            Secret = "test-webhook-secret-16",
            SubscribedEvents = new List<string> { "WorkflowStarted" }
        }, CancellationToken.None);
        createResult.Should().BeOfType<CreatedAtActionResult>();

        // Act - Delete
        var deleteResult = await controller.DeleteWebhook(subId, CancellationToken.None);
        deleteResult.Should().BeOfType<OkObjectResult>();
    }
}
