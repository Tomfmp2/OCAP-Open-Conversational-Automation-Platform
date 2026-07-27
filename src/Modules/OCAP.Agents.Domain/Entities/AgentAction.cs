namespace OCAP.Agents.Domain.Entities;

// Representa una acción determinada por el agente a ser ejecutada como respuesta o automatización.
public class AgentAction
{
    // Tipos de acciones predefinidas en el sistema.
    public const string CreateCalendarEvent = "CreateCalendarEvent";
    public const string SendEmail = "SendEmail";
    public const string CreateReminder = "CreateReminder";
    public const string TransferToHuman = "TransferToHuman";
    public const string GenerateResponse = "GenerateResponse";

    // Tipo o identificador de la acción a ejecutar.
    public string ActionType { get; }

    // Nombre opcional de la herramienta externa a invocar (ej. "GoogleCalendarTool").
    public string? TargetToolName { get; }

    // Argumentos requeridos para ejecutar la acción.
    public IReadOnlyDictionary<string, object> Parameters { get; }

    // Fecha y hora de determinación de la acción.
    public DateTime CreatedAt { get; } = DateTime.UtcNow;

    public AgentAction(string actionType, string? targetToolName = null, Dictionary<string, object>? parameters = null)
    {
        ActionType = string.IsNullOrWhiteSpace(actionType) ? GenerateResponse : actionType.Trim();
        TargetToolName = targetToolName;
        Parameters = parameters ?? new Dictionary<string, object>();
    }
}
