namespace OCAP.Intelligence.Abstractions;

// Modelo que representa el estado de salud, latencia y disponibilidad de un proveedor de IA.
public class ProviderHealth
{
    public string ProviderName { get; set; } = string.Empty;
    public bool IsHealthy { get; set; }
    public double LatencyMs { get; set; }
    public List<string> ModelList { get; set; } = new();
    public string Version { get; set; } = "1.0.0";
    public string StatusMessage { get; set; } = "OK";
    public DateTime CheckedAtUtc { get; set; } = DateTime.UtcNow;
}
