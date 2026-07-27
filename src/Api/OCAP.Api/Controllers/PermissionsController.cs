using Microsoft.AspNetCore.Mvc;
using OCAP.Api.Models.Security;

namespace OCAP.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PermissionsController : ControllerBase
{
    [HttpGet]
    public ActionResult<List<PermissionDto>> GetPermissions()
    {
        var permissions = new List<PermissionDto>
        {
            new(Guid.NewGuid(), "Conversation.Read", "Lectura de Conversaciones", "Conversations", "Permite ver el historial de conversaciones"),
            new(Guid.NewGuid(), "Conversation.Write", "Escritura de Conversaciones", "Conversations", "Permite enviar mensajes"),
            new(Guid.NewGuid(), "Conversation.Delete", "Eliminación de Conversaciones", "Conversations", "Permite eliminar conversaciones"),
            new(Guid.NewGuid(), "Agent.Read", "Lectura de Agentes", "Agents", "Permite ver catálogo de agentes"),
            new(Guid.NewGuid(), "Agent.Write", "Edición de Agentes", "Agents", "Permite modificar agentes"),
            new(Guid.NewGuid(), "Agent.Execute", "Ejecución de Agentes", "Agents", "Permite ejecutar razonamiento de agente"),
            new(Guid.NewGuid(), "Tool.Execute", "Ejecución de Herramientas", "Tools", "Permite invocar herramientas externas"),
            new(Guid.NewGuid(), "Dashboard.Read", "Lectura del Dashboard", "Dashboard", "Permite ver métricas del dashboard"),
            new(Guid.NewGuid(), "Dashboard.Admin", "Administración del Dashboard", "Dashboard", "Acceso total a funciones administrativas"),
            new(Guid.NewGuid(), "Deployment.Manage", "Gestión de Despliegue", "Deployment", "Permite administrar el autohospedaje"),
            new(Guid.NewGuid(), "AI.Execute", "Ejecución de IA", "AI", "Permite utilizar modelos de IA Generativa"),
            new(Guid.NewGuid(), "Settings.Manage", "Gestión de Configuración", "Settings", "Permite modificar reglas del sistema"),
            new(Guid.NewGuid(), "OAuth.Manage", "Gestión OAuth2", "Security", "Permite conectar credenciales OAuth")
        };
        return Ok(permissions);
    }
}
