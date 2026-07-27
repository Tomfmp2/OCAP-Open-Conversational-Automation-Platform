namespace OCAP.Api.Models.Dashboard;

// DTO para el monitoreo del estado de integraciones empresariales de Google Workspace.
public class GoogleIntegrationDto
{
    public bool IsConnected { get; set; } = true;
    public string AccountEmail { get; set; } = "workspace-admin@ocap.org";
    public string OAuthStatus { get; set; } = "Authorized";
    public List<string> GrantedScopes { get; set; } = new() { "Calendar.Create", "Gmail.Send", "Sheets.Append" };
    public DateTime LastSyncedAt { get; set; } = DateTime.UtcNow;
}
