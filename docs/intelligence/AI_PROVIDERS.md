# Modelos de IA y proveedores

## Visión General

OCAP adopta la estrategia **Provider Agnostic**: la plataforma no depende de un proveedor de Inteligencia Artificial específico. Todos los adaptadores implementan la interfaz `IAiProvider`.

---

## Proveedores disponibles

| Proveedor | Módulo | Estado | Notas |
|---|---|---|---|
| **Gemini** | `OCAP.Providers.Gemini` | Listo | `gemini-3.5-flash` por defecto; texto, imágenes y video adjuntos; embeddings |
| **OpenAI** | `OCAP.Providers.OpenAI` | Listo | Texto e imágenes; embeddings |
| **Compatible con OpenAI** | `OCAP.Providers.OpenAI` | Listo | NVIDIA, Groq, OpenRouter, DeepSeek, Mistral u otra API con el formato de OpenAI y su dirección (`baseUrl`) |
| **Claude** | `OCAP.Providers.Claude` | Listo | Texto e imágenes |
| **Ollama** | `OCAP.Providers.Ollama` | Listo | Local, en tu equipo o tu red; imágenes con modelos de visión |
| **Mock (modo demo)** | `OCAP.Intelligence.Application` | Listo | Responde «[Modo demo]» mientras no hay ningún modelo configurado |

---

## Modelos desde el panel

En **Modelos de IA** cada organización agrega sus modelos (`/api/aiproviderconfigurations`). La API key se guarda
cifrada (AES-256-GCM) en el vault de la organización y nunca vuelve al navegador; la respuesta solo trae
`vault:stored` cuando hay key. Las rutas `tenant/{tenantId}` responden 404 si el tenant no es el de la sesión.

- **Varias configuraciones por proveedor** (otra clave, otro modelo, otra API compatible). Las distingue el
  **nombre**, único por organización.
- **Compatible con OpenAI** exige la dirección de la API (`baseUrl`). Esa dirección y la de Ollama las escribe un
  usuario: la API solo llega a destinos públicos (Ollama también a la red privada), vuelve a comprobar la IP al
  conectar y no sigue redirecciones.
- **Ver modelos disponibles**: `POST /api/aiproviderconfigurations/models` lista los modelos del proveedor, con la
  clave guardada (`configurationId`) o con la que se está escribiendo (no se guarda).
- **Predeterminado**: `POST /api/providers/select` con el id de la configuración. Probar un modelo no lo convierte
  en predeterminado.

## Modelo por canal

Cada canal puede usar un modelo concreto: `PUT /api/channels/connections/{id}/ai` con
`{ "aiProviderConfigId": "<id>" | null }` (400 si no es de la organización o está deshabilitado). Al responder se
usa, en orden:

1. El modelo asignado al canal, si sigue habilitado y tiene key.
2. Si no, el predeterminado de la organización (el mismo que usa el asistente del panel).

Así, quitar o deshabilitar el modelo de un canal no lo deja sin respuesta.

## Consumo

- Un `429` (cuota agotada) no se reintenta: solo gastaría otra petición. Se reintentan fallos de red y `5xx`.
- Sin documentos indexados no se llama a la API de embeddings.
- La memoria y los resúmenes se procesan en segundo plano, una vez por conversación terminada
  ([MEMORY.md](../guides/MEMORY.md)).

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
