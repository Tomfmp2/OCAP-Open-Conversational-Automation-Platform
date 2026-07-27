namespace OCAP.Tools.Abstractions;

// Resultado estandarizado de la ejecución de una herramienta.
public class ToolExecutionResult
{
    // Indica si la ejecución de la herramienta fue exitosa.
    public bool Success { get; set; }

    // Objeto con los datos o respuesta producidos por la herramienta.
    public object? Output { get; set; }

    // Mensaje descriptivo del error en caso de falla.
    public string? ErrorMessage { get; set; }

    // Fecha y hora en que culminó la ejecución.
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;

    // Métodos fábrica estáticos para construir resultados comunes.
    public static ToolExecutionResult Ok(object? output) => new() { Success = true, Output = output };
    public static ToolExecutionResult Fail(string error) => new() { Success = false, ErrorMessage = error };
}
