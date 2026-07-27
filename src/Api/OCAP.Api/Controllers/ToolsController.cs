using Microsoft.AspNetCore.Mvc;
using OCAP.Api.Models.Dashboard;

namespace OCAP.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
// Controlador de API que expone el catálogo de herramientas disponibles en OCAP.
public class ToolsController : ControllerBase
{
    [HttpGet]
    public ActionResult<IEnumerable<ToolDto>> GetTools()
    {
        var tools = new List<ToolDto>
        {
            new ToolDto
            {
                Id = "google.calendar.create_event",
                Name = "CreateCalendarEventTool",
                Description = "Crea un evento en Google Calendar.",
                Version = "1.0.0",
                Status = "Active",
                RequiredPermissions = new List<string> { "Calendar.Create" }
            },
            new ToolDto
            {
                Id = "google.gmail.send_email",
                Name = "SendEmailTool",
                Description = "Envía correos electrónicos mediante Gmail.",
                Version = "1.0.0",
                Status = "Active",
                RequiredPermissions = new List<string> { "Gmail.Send" }
            },
            new ToolDto
            {
                Id = "google.sheets.append_row",
                Name = "AppendSpreadsheetRowTool",
                Description = "Anexa datos a hojas de cálculo en Google Sheets.",
                Version = "1.0.0",
                Status = "Active",
                RequiredPermissions = new List<string> { "Sheets.Append" }
            }
        };

        return Ok(tools);
    }
}
