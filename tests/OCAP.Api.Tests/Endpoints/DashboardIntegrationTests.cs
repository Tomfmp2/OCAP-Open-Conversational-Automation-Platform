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
    public void WorkflowsController_ExposesManagementAndStatusEndpoints()
    {
        // Arrange
        var engineMock = new Mock<IWorkflowEngine>();
        var validatorMock = new Mock<IWorkflowValidator>();
        var mapperMock = new Mock<IWorkflowDesignerMapper>();

        var controller = new WorkflowsController(engineMock.Object, validatorMock.Object, mapperMock.Object);
        var workflowId = Guid.NewGuid();

        // Act - GetById
        var getByIdResult = controller.GetWorkflowById(workflowId);
        getByIdResult.Result.Should().BeOfType<OkObjectResult>();

        // Act - Status
        var statusResult = controller.GetWorkflowStatus(workflowId);
        statusResult.Result.Should().BeOfType<OkObjectResult>();

        // Act - Executions
        var executionsResult = controller.GetExecutionsForWorkflow(workflowId);
        executionsResult.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public void AgentsController_ExposesAgentListDetailsAndRuntimeStatus()
    {
        // Arrange
        var controller = new AgentsController();
        var agentId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        // Act - List
        var listResult = controller.GetAgents();
        listResult.Result.Should().BeOfType<OkObjectResult>();

        // Act - Details
        var detailsResult = controller.GetAgentById(agentId);
        detailsResult.Result.Should().BeOfType<OkObjectResult>();

        // Act - Runtime Status
        var statusResult = controller.GetAgentRuntimeStatus(agentId);
        statusResult.Result.Should().BeOfType<OkObjectResult>();
        var okStatus = (OkObjectResult)statusResult.Result!;
        var statusDto = okStatus.Value.Should().BeOfType<AgentRuntimeStatusDto>().Subject;
        statusDto.Status.Should().Be("Operational");
    }

    [Fact]
    public void ChannelManagementController_ExposesProviderStatusHealthConfigurationAndStatistics()
    {
        // Arrange
        var registryMock = new Mock<IChannelRegistry>();
        var connManagerMock = new Mock<IChannelConnectionManager>();
        var tenantMock = new Mock<ITenantContext>();
        var userMock = new Mock<IUserContext>();
        var permMock = new Mock<IPermissionValidator>();
        var auditMock = new Mock<ISecurityAuditService>();

        var controller = new ChannelManagementController(
            registryMock.Object, connManagerMock.Object, tenantMock.Object, userMock.Object, permMock.Object, auditMock.Object);

        // Act - Telegram Status & Health
        var telegramStatus = controller.GetProviderStatus("Telegram") as OkObjectResult;
        telegramStatus.Should().NotBeNull();
        telegramStatus!.StatusCode.Should().Be(200);

        var telegramHealth = controller.GetProviderHealth("Telegram") as OkObjectResult;
        telegramHealth.Should().NotBeNull();

        // Act - WhatsApp Config & Statistics
        var waConfig = controller.GetProviderConfiguration("WhatsApp") as OkObjectResult;
        waConfig.Should().NotBeNull();

        var waStats = controller.GetProviderStatistics("WhatsApp") as OkObjectResult;
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
            SubscribedEvents = new List<string> { "WorkflowStarted" }
        }, CancellationToken.None);
        createResult.Should().BeOfType<CreatedAtActionResult>();

        // Act - Delete
        var deleteResult = await controller.DeleteWebhook(subId, CancellationToken.None);
        deleteResult.Should().BeOfType<OkObjectResult>();
    }
}
