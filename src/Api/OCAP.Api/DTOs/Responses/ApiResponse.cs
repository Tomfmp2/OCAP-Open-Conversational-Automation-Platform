namespace OCAP.Api.DTOs.Responses;

// Respuesta genérica para la API
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
}
