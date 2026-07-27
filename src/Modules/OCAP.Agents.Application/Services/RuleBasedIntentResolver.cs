using OCAP.Agents.Abstractions.Ports;
using OCAP.Agents.Domain.Entities;

namespace OCAP.Agents.Application.Services;

// Resolver de intenciones basado en reglas heurísticas y palabras clave.
// Proporciona una clasificación determinista antes de integrar modelos de Inteligencia Artificial (LLMs).
public class RuleBasedIntentResolver : IIntentResolver
{
    public Task<Intent> ResolveIntentAsync(string message, ConversationContext? context, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return Task.FromResult(Intent.CreateUnknown());
        }

        var clean = message.ToLowerInvariant().Trim();

        // 1. Detección de saludos
        if (ContainsAny(clean, "hola", "buenos dias", "buenas tardes", "buenas noches", "hello", "hi", "saludos"))
        {
            return Task.FromResult(new Intent(Intent.Greeting, 0.95));
        }

        // 2. Detección de creación de recordatorios o eventos
        if (ContainsAny(clean, "recordar", "recuerdame", "recordatorio", "agendar", "cita", "reunion", "evento"))
        {
            var paramsDict = new Dictionary<string, string>
            {
                ["OriginalQuery"] = message
            };
            return Task.FromResult(new Intent(Intent.CreateReminder, 0.90, paramsDict));
        }

        // 3. Detección de intervención humana o transferencia a asesor
        if (ContainsAny(clean, "humano", "asesor", "persona", "agente humano", "soporte real", "transferir"))
        {
            return Task.FromResult(new Intent(Intent.HumanSupport, 0.99));
        }

        // 4. Detección de solicitud de información o ayuda
        if (ContainsAny(clean, "informacion", "ayuda", "que es", "como funciona", "opciones", "menu"))
        {
            return Task.FromResult(new Intent(Intent.GetInformation, 0.85));
        }

        // Intención no reconocida
        return Task.FromResult(Intent.CreateUnknown());
    }

    private static bool ContainsAny(string source, params string[] keywords)
    {
        return keywords.Any(k => source.Contains(k, StringComparison.OrdinalIgnoreCase));
    }
}
