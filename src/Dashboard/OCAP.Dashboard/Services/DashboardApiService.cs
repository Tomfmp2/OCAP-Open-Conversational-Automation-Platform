using System.Net.Http.Json;
using OCAP.Dashboard.Models;

namespace OCAP.Dashboard.Services;

// Implementación del servicio API que consume endpoints de la API Gateway mediante HttpClient.
public class DashboardApiService : IDashboardApiService
{
    private readonly HttpClient _http;

    public DashboardApiService(HttpClient http)
    {
        _http = http;
    }

    public async Task<DashboardStatusModel> GetStatusAsync()
    {
        var result = await _http.GetFromJsonAsync<DashboardStatusModel>("api/dashboard/status");
        return result ?? new DashboardStatusModel();
    }

    public async Task<DashboardMetricsModel> GetMetricsAsync()
    {
        var result = await _http.GetFromJsonAsync<DashboardMetricsModel>("api/dashboard/metrics");
        return result ?? new DashboardMetricsModel();
    }

    public async Task<List<AgentModel>> GetAgentsAsync()
    {
        var result = await _http.GetFromJsonAsync<List<AgentModel>>("api/agents");
        return result ?? new List<AgentModel>();
    }

    public async Task<List<ToolModel>> GetToolsAsync()
    {
        var result = await _http.GetFromJsonAsync<List<ToolModel>>("api/tools");
        return result ?? new List<ToolModel>();
    }

    public async Task<List<ExecutionModel>> GetExecutionsAsync()
    {
        var result = await _http.GetFromJsonAsync<List<ExecutionModel>>("api/executions");
        return result ?? new List<ExecutionModel>();
    }

    public async Task<List<ConversationModel>> GetConversationsAsync()
    {
        var result = await _http.GetFromJsonAsync<List<ConversationModel>>("api/conversations");
        return result ?? new List<ConversationModel>();
    }

    public async Task<GoogleIntegrationModel> GetGoogleIntegrationAsync()
    {
        var result = await _http.GetFromJsonAsync<GoogleIntegrationModel>("api/integrations/google");
        return result ?? new GoogleIntegrationModel();
    }
}
