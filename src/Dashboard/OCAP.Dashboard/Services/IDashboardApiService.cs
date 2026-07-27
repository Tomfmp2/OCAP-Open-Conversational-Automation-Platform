using OCAP.Dashboard.Models;

namespace OCAP.Dashboard.Services;

// Contrato de servicio HTTP del frontend para consultar la API Gateway de OCAP.
public interface IDashboardApiService
{
    Task<DashboardStatusModel> GetStatusAsync();
    Task<DashboardMetricsModel> GetMetricsAsync();
    Task<List<AgentModel>> GetAgentsAsync();
    Task<List<ToolModel>> GetToolsAsync();
    Task<List<ExecutionModel>> GetExecutionsAsync();
    Task<List<ConversationModel>> GetConversationsAsync();
    Task<GoogleIntegrationModel> GetGoogleIntegrationAsync();
}
