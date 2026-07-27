namespace OCAP.Api.Models.Dashboard;

// DTO con el estado operativo del motor de Inteligencia Artificial Generativa.
public record AiStatusDto(
    string ActiveProvider,
    string ActiveModel,
    string Status,
    DateTime LastCheckedUtc
);

// DTO con métricas globales de consumo de tokens e invocaciones de IA.
public record AiUsageDto(
    int TotalTokensUsed,
    int TotalExecutionsCount,
    double AverageLatencyMs,
    double SuccessRatePercentage
);

// DTO con detalles e información de capacidades de un modelo de IA.
public record AiModelInfoDto(
    string Provider,
    string Model,
    int ContextSize,
    List<string> Capabilities
);
