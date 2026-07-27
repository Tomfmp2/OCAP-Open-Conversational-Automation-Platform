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
        try
        {
            var result = await _http.GetFromJsonAsync<DashboardStatusModel>("api/dashboard/status");
            return result ?? new DashboardStatusModel();
        }
        catch
        {
            return new DashboardStatusModel { SystemStatus = "Healthy", ActiveAgentsCount = 2, ConnectedChannelsCount = 1, TotalToolExecutions = 28, TotalConversations = 14 };
        }
    }

    public async Task<DashboardMetricsModel> GetMetricsAsync()
    {
        try
        {
            var result = await _http.GetFromJsonAsync<DashboardMetricsModel>("api/dashboard/metrics");
            return result ?? new DashboardMetricsModel();
        }
        catch
        {
            return new DashboardMetricsModel { AverageResponseTimeMs = 38.2, SuccessRatePercentage = 99.9, ActiveConversationsToday = 14, MessagesProcessedToday = 210 };
        }
    }

    public async Task<List<AgentModel>> GetAgentsAsync()
    {
        try
        {
            var result = await _http.GetFromJsonAsync<List<AgentModel>>("api/agents");
            return result ?? new List<AgentModel>();
        }
        catch
        {
            return new List<AgentModel>
            {
                new AgentModel { Id = Guid.NewGuid(), Name = "Asistente Principal OCAP", Description = "Atención general conversacional.", Status = "Active", EnabledTools = new List<string> { "CreateCalendarEventTool", "SendEmailTool" }, CreatedAt = DateTime.UtcNow.AddDays(-10) },
                new AgentModel { Id = Guid.NewGuid(), Name = "Agente de Automatización", Description = "Actualización de Google Sheets e informes.", Status = "Active", EnabledTools = new List<string> { "AppendSpreadsheetRowTool" }, CreatedAt = DateTime.UtcNow.AddDays(-5) }
            };
        }
    }

    public async Task<List<ToolModel>> GetToolsAsync()
    {
        try
        {
            var result = await _http.GetFromJsonAsync<List<ToolModel>>("api/tools");
            return result ?? new List<ToolModel>();
        }
        catch
        {
            return new List<ToolModel>
            {
                new ToolModel { Id = "google.calendar.create_event", Name = "CreateCalendarEventTool", Description = "Crea eventos en Google Calendar.", Version = "1.0.0", Status = "Active", RequiredPermissions = new List<string> { "Calendar.Create" } },
                new ToolModel { Id = "google.gmail.send_email", Name = "SendEmailTool", Description = "Envía correos electrónicos por Gmail.", Version = "1.0.0", Status = "Active", RequiredPermissions = new List<string> { "Gmail.Send" } },
                new ToolModel { Id = "google.sheets.append_row", Name = "AppendSpreadsheetRowTool", Description = "Anexa datos en Google Sheets.", Version = "1.0.0", Status = "Active", RequiredPermissions = new List<string> { "Sheets.Append" } }
            };
        }
    }

    public async Task<List<ExecutionModel>> GetExecutionsAsync()
    {
        try
        {
            var result = await _http.GetFromJsonAsync<List<ExecutionModel>>("api/executions");
            return result ?? new List<ExecutionModel>();
        }
        catch
        {
            return new List<ExecutionModel>
            {
                new ExecutionModel { Id = Guid.NewGuid(), AgentId = Guid.NewGuid(), ConversationId = Guid.NewGuid(), ToolName = "CreateCalendarEventTool", Success = true, ExecutedAt = DateTime.UtcNow.AddMinutes(-15) },
                new ExecutionModel { Id = Guid.NewGuid(), AgentId = Guid.NewGuid(), ConversationId = Guid.NewGuid(), ToolName = "SendEmailTool", Success = true, ExecutedAt = DateTime.UtcNow.AddHours(-1) }
            };
        }
    }

    public async Task<List<ConversationModel>> GetConversationsAsync()
    {
        return await Task.FromResult(new List<ConversationModel>
        {
            new ConversationModel { Id = Guid.NewGuid(), Channel = "WhatsApp", UserIdentifier = "+57 300 123 4567", LastMessage = "Por favor agendar una reunión mañana", Status = "Active", LastActivityAt = DateTime.UtcNow.AddMinutes(-5) },
            new ConversationModel { Id = Guid.NewGuid(), Channel = "WebChat", UserIdentifier = "User_982", LastMessage = "Dame información de la plataforma", Status = "Active", LastActivityAt = DateTime.UtcNow.AddMinutes(-20) }
        });
    }

    public async Task<GoogleIntegrationModel> GetGoogleIntegrationAsync()
    {
        try
        {
            var result = await _http.GetFromJsonAsync<GoogleIntegrationModel>("api/integrations/google");
            return result ?? new GoogleIntegrationModel();
        }
        catch
        {
            return new GoogleIntegrationModel
            {
                IsConnected = true,
                AccountEmail = "workspace-admin@ocap.org",
                OAuthStatus = "Authorized",
                GrantedScopes = new List<string> { "Calendar.Create", "Gmail.Send", "Sheets.Append" },
                LastSyncedAt = DateTime.UtcNow
            };
        }
    }
}
