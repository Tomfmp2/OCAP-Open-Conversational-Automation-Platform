# OCAP — Guía de Proveedores de IA (AI Providers Guide)

## Visión General

OCAP adopta la estrategia **Provider Agnostic**: la plataforma no depende de un proveedor de Inteligencia Artificial específico. Todos los adaptadores implementan la interfaz `IAiProvider`.

---

## Proveedores Disponibles y En preparación

| Proveedor | Módulo | Estado | Descripción |
|---|---|---|---|
| **MockAI** | `OCAP.Intelligence.Mock` | **Activo (Default)** | Motor mock local para pruebas automatizadas y desarrollo offline. |
| **OpenAI** | `OCAP.Providers.OpenAI` | **Preparado** | Adaptador para modelos GPT-4o / GPT-3.5 Turbo. |
| **Google Gemini** | `OCAP.Providers.Gemini` | **Preparado** | Adaptador para modelos Gemini 3.5 / 2.5 Flash. |
| **Anthropic Claude** | `OCAP.Providers.Claude` | **Preparado** | Adaptador para Claude (por defecto `claude-sonnet-5-5`). |
| **Ollama Local** | `OCAP.Providers.Ollama` | **Preparado** | Adaptador self-hosted para Llama 3, Mistral u otros LLMs locales. |

---

## Keys desde el panel y proveedor por canal

En **IA y modelos** cada organización registra sus proveedores (`/api/aiproviderconfigurations`). La API key se
guarda cifrada en el vault de la organización y nunca vuelve al navegador: la respuesta solo trae `vault:stored`
cuando hay key. Hay una configuración por tipo de proveedor y organización; registrar otra del mismo tipo responde
422 (se edita la existente). Las rutas `tenant/{tenantId}` responden 404 si el tenant no es el de la sesión.

Cada canal puede usar un proveedor concreto: `PUT /api/channels/connections/{id}/ai` con
`{ "aiProviderConfigId": "<id>" | null }` (400 si no es de la organización o está deshabilitado). Se guarda en la
metadata `AiProviderConfigId` de la conexión. Al responder un mensaje del canal se usa, en orden:

1. El proveedor asignado al canal, si sigue habilitado y tiene key.
2. Si no, el proveedor activo de la organización (el mismo que usa el asistente del panel).

Así, quitar o deshabilitar el proveedor de un canal no lo deja sin respuesta.

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
