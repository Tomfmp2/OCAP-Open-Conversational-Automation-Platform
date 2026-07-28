namespace OCAP.Intelligence.Abstractions;

// Registro centralizado y fábrica de instancias de proveedores de IA.
public interface IAiProviderRegistry
{
    // Registra o actualiza una instancia de proveedor por su nombre.
    void RegisterProvider(IAiProvider provider);

    // Obtiene una instancia de proveedor por su nombre registrado ("OpenAI", "Gemini", "Ollama", "Local", "Mock").
    IAiProvider? GetProvider(string providerName);

    // Obtiene la lista de nombres de todos los proveedores registrados en la plataforma.
    IReadOnlyList<string> GetRegisteredProviderNames();

    // Crea una instancia dinámica de proveedor configurada con parámetros específicos y credenciales descifradas.
    IAiProvider CreateDynamicProvider(string providerName, string modelName, string apiKey, string? baseUrl = null);
}
