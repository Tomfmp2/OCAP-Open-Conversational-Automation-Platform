namespace OCAP.Intelligence.Abstractions;

// Modelo de solicitud estandarizado para consultar un proveedor de Inteligencia Artificial.
public class AiRequest
{
    public Guid AgentId { get; set; }
    public Guid ConversationId { get; set; }
    public string UserMessage { get; set; } = string.Empty;
    public List<string> ConversationHistory { get; set; } = new();
    public string SystemInstructions { get; set; } = string.Empty;
    public double? Temperature { get; set; }
    public int? MaxTokens { get; set; }
    public string ResponseFormat { get; set; } = "text"; // "text" o "json_object"
    public bool EnableStreaming { get; set; }
    public List<string> AvailableTools { get; set; } = new();
    public Dictionary<string, object> Parameters { get; set; } = new();
    /// <summary>Imágenes o video del mensaje del usuario (los documentos llegan ya como texto).</summary>
    public List<AiAttachment> Attachments { get; set; } = new();
}

/// <summary>Archivo para un modelo multimodal. Cada proveedor indica qué tipos admite.</summary>
public sealed record AiAttachment(string MimeType, string Base64Data)
{
    public bool IsImage => MimeType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
}

/// <summary>El modelo elegido no puede leer ese tipo de adjunto (p. ej. video fuera de Gemini).</summary>
public sealed class UnsupportedAttachmentException(string message) : NotSupportedException(message);
