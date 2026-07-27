namespace OCAP.Api.Models.Providers;

// DTO para la información de un proveedor de IA.
public record ProviderInfoDto(string Name, string DefaultModel, bool IsActive, int Priority);

// DTO para seleccionar el proveedor activo.
public record SelectProviderRequestDto(string ProviderName);

// DTO para probar la respuesta de un proveedor de IA.
public record TestProviderRequestDto(string Prompt, string? ProviderName, double? Temperature, int? MaxTokens);

// DTO de resultado de prueba con métricas de observabilidad.
public record TestProviderResponseDto(
    string ProviderUsed,
    string ModelUsed,
    string GeneratedText,
    int TokensUsed,
    double LatencyMs,
    double EstimatedCostUsd,
    bool FromCache
);
