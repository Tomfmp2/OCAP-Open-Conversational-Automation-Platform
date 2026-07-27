namespace OCAP.Tools.Abstractions;

// Resultado estandarizado de la ejecución de una herramienta.
public class ToolResult
{
    // Indica si la ejecución de la herramienta culminó con éxito.
    public bool Success { get; }

    // Código de error estandarizado cuando Success es false.
    public string? ErrorCode { get; }

    // Mensaje explicativo del resultado o error.
    public string? Message { get; }

    // Datos de salida producidos por la herramienta.
    public object? Data { get; }

    // Metadatos adicionales de telemetría o ejecución.
    public IReadOnlyDictionary<string, object>? Metadata { get; }

    private ToolResult(bool success, string? errorCode, string? message, object? data, Dictionary<string, object>? metadata)
    {
        Success = success;
        ErrorCode = errorCode;
        Message = message;
        Data = data;
        Metadata = metadata;
    }

    // Métodos fábrica estáticos para construir resultados de ejecución.
    public static ToolResult Ok(object? data = null, string? message = null, Dictionary<string, object>? metadata = null)
        => new(true, null, message ?? "Ejecución completada con éxito.", data, metadata);

    public static ToolResult Fail(string errorCode, string message, Dictionary<string, object>? metadata = null)
        => new(false, errorCode, message, null, metadata);
}
