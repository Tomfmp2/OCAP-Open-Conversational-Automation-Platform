namespace OCAP.Intelligence.Abstractions;

/// <summary>
/// Modo demo: no hay ninguna API key de IA en la configuración del servidor, así que el asistente responde con el
/// proveedor <c>Mock</c> en vez de fallar. Una organización que configure su proveedor en el panel (IA y modelos)
/// usa ese proveedor igualmente: el modo demo solo cambia el proveedor estático por defecto.
/// </summary>
public sealed record AiDemoMode(bool Enabled)
{
    public const string MockProviderName = "Mock";
}
