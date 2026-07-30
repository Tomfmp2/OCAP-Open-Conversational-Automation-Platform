using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using OCAP.Api.Models.Dashboard;
using OCAP.Api.Tests.Infrastructure;

namespace OCAP.Api.Tests.Endpoints;

public class DashboardEndpointTests : IClassFixture<OcapApiFactory>
{
    private readonly HttpClient _client;

    public DashboardEndpointTests(OcapApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetStatus_Returns200OkWithStatusDto()
    {
        // Act
        var response = await _client.GetAsync("/api/dashboard/status");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var dto = await response.Content.ReadFromJsonAsync<DashboardStatusDto>();
        dto.Should().NotBeNull();
        dto!.SystemStatus.Should().Be("Healthy");
    }

    [Fact]
    public async Task GetMetrics_Returns200OkWithMetricsDto()
    {
        // Act
        var response = await _client.GetAsync("/api/dashboard/metrics");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var dto = await response.Content.ReadFromJsonAsync<DashboardMetricsDto>();
        dto.Should().NotBeNull();
        dto!.AverageResponseTimeMs.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetAgents_Returns200OkWithAgentsList()
    {
        // Act
        var response = await _client.GetAsync("/api/agents");

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, content);

        var list = await response.Content.ReadFromJsonAsync<List<AgentDto>>();
        list.Should().NotBeNull();
    }

    [Fact]
    public async Task GetTools_Returns200OkWithToolsList()
    {
        // Act
        var response = await _client.GetAsync("/api/tools");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var list = await response.Content.ReadFromJsonAsync<List<ToolDto>>();
        list.Should().NotBeNull();
        list.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetExecutions_Returns200OkWithExecutionsList()
    {
        // Act
        var response = await _client.GetAsync("/api/executions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var list = await response.Content.ReadFromJsonAsync<List<ExecutionDto>>();
        list.Should().NotBeNull();
    }

    [Fact]
    public async Task GetGoogleIntegration_Returns200OkWithIntegrationDto()
    {
        // Act
        var response = await _client.GetAsync("/api/integrations/google");

        // Assert - include body in failure message for diagnosing non-200 responses
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, $"Response body: {body}");

        var dto = await response.Content.ReadFromJsonAsync<GoogleIntegrationDto>();
        dto.Should().NotBeNull();
        dto!.IsConnected.Should().BeTrue();
    }
}
