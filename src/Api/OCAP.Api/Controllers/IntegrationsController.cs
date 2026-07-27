using Microsoft.AspNetCore.Mvc;
using OCAP.Api.Models.Dashboard;

namespace OCAP.Api.Controllers;

[ApiController]
[Route("api/integrations")]
// Controlador de API para consultar el estado de integraciones empresariales externas.
public class IntegrationsController : ControllerBase
{
    [HttpGet("google")]
    public ActionResult<GoogleIntegrationDto> GetGoogleIntegration()
    {
        var integration = new GoogleIntegrationDto
        {
            IsConnected = true,
            AccountEmail = "workspace-admin@ocap.org",
            OAuthStatus = "Authorized",
            GrantedScopes = new List<string> { "Calendar.Create", "Gmail.Send", "Sheets.Append" },
            LastSyncedAt = DateTime.UtcNow
        };

        return Ok(integration);
    }
}
