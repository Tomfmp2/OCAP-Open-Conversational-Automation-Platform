namespace OCAP.Intelligence.Abstractions;

// Prioridades soportadas para la selección dinámica de proveedor.
public enum ProviderPriority
{
    Primary = 1,
    Secondary = 2,
    Tertiary = 3
}

// Criterios de orquestación inteligente de proveedores de IA.
public class ProviderPolicy
{
    public string ProviderName { get; set; } = string.Empty;
    public ProviderPriority Priority { get; set; } = ProviderPriority.Primary;
    public double EstimatedCostPer1kTokens { get; set; } = 0.002;
    public bool EnableFailover { get; set; } = true;
    public int MaxRetries { get; set; } = 3;
    public int TimeoutSeconds { get; set; } = 30;
}
