namespace OCAP.Channels.Abstractions.Contracts;

// Resultado con diagnóstico detallado sobre la disponibilidad y salud de una conexión de canal.
public class ChannelHealthResult
{
    public bool IsHealthy { get; set; }
    public string StatusMessage { get; set; } = string.Empty;
    public DateTime CheckedAtUtc { get; set; } = DateTime.UtcNow;
    public Dictionary<string, string> HealthDetails { get; set; } = new();
}

// Contrato para verificar la salud y disponibilidad operativa de proveedores externos de comunicación.
public interface IChannelHealthChecker
{
    Task<ChannelHealthResult> CheckHealthAsync(Guid tenantId, string provider, CancellationToken cancellationToken = default);
}
