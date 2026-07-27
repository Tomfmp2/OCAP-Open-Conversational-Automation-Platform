# OCAP — Guía de Proveedores de IA (AI Providers Guide)

## Visión General

OCAP adopta la estrategia **Provider Agnostic**: la plataforma no depende de un proveedor de Inteligencia Artificial específico. Todos los adaptadores implementan la interfaz `IAiProvider`.

---

## Proveedores Disponibles y En preparación

| Proveedor | Módulo | Estado | Descripción |
|---|---|---|---|
| **MockAI** | `OCAP.Intelligence.Mock` | **Activo (Default)** | Motor mock local para pruebas automatizadas y desarrollo offline. |
| **OpenAI** | `OCAP.Providers.OpenAI` | **Preparado** | Adaptador para modelos GPT-4o / GPT-3.5 Turbo. |
| **Google Gemini** | `OCAP.Providers.Gemini` | **Preparado** | Adaptador para modelos Gemini 1.5 Pro / Flash. |
| **Ollama Local** | `OCAP.Providers.Ollama` | **Preparado** | Adaptador self-hosted para Llama 3, Mistral u otros LLMs locales. |

---

## Cómo Crear un Nuevo Proveedor de IA

Para incorporar un nuevo servicio de IA Generativa en OCAP:

1. Crear un proyecto en `src/Providers/OCAP.Providers.<Nombre>`.
2. Referenciar `OCAP.Intelligence.Abstractions` y `OCAP.Intelligence.Domain`.
3. Crear la clase que implemente `IAiProvider`:

```csharp
namespace OCAP.Providers.Custom;

public class CustomAiProvider : IAiProvider
{
    private readonly AiProviderSettings _settings;

    public CustomAiProvider(AiProviderSettings settings)
    {
        _settings = settings;
    }

    public async Task<AiResponse> GenerateResponseAsync(AiRequest request, CancellationToken cancellationToken = default)
    {
        // 1. Mapear AiRequest al formato del SDK del proveedor
        // 2. Invocar la API remota o servidor local
        // 3. Retornar AiResponse estandarizado
    }

    public async Task<Intent> AnalyzeIntentAsync(string message, CancellationToken cancellationToken = default)
    {
        // Analizar la intención del usuario
    }

    public Task<T?> ExtractStructuredDataAsync<T>(string text, CancellationToken cancellationToken = default) => ...;

    public AiModelInformation GetModelInformation() => ...;
}
```

4. Registrar el nuevo proveedor en `ApiServiceExtensions.cs` mediante Inyección de Dependencias.
