using System.Text.Json;
using OCAP.Agents.Domain.Entities;
using OCAP.Intelligence.Abstractions;
using OCAP.Intelligence.Domain;

namespace OCAP.Intelligence.Mock;

// Proveedor mock de Inteligencia Artificial para ejecución de pruebas sin servicios externos.
public class MockAiProvider : IAiProvider
{
    public Task<AiResponse> GenerateResponseAsync(AiRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));

        var text = request.UserMessage.ToLowerInvariant();
        string generatedText;

        if (text.Contains("reunión") || text.Contains("cita") || text.Contains("agendar") || text.Contains("recordatorio"))
        {
            generatedText = "Comprendo tu solicitud. Procederé a agendar la reunión solicitada en el calendario empresarial.";
        }
        else if (text.Contains("correo") || text.Contains("email") || text.Contains("enviar"))
        {
            generatedText = "Entendido. Voy a redactar y enviar el correo electrónico especificado.";
        }
        else if (text.Contains("hoja") || text.Contains("excel") || text.Contains("sheet") || text.Contains("tabla"))
        {
            generatedText = "Procesando los datos para registrarlos en la hoja de cálculo especificada.";
        }
        else
        {
            generatedText = $"[Respuesta Mock IA]: He procesado tu mensaje: '{request.UserMessage}'. ¿Deseas realizar alguna otra acción?";
        }

        var response = new AiResponse
        {
            GeneratedText = generatedText,
            TokensUsed = 42,
            ModelName = "mock-gpt-4o",
            ProviderName = "MockAI",
            Metadata = new Dictionary<string, object>
            {
                ["LatencyMs"] = 15.5,
                ["SimulationMode"] = true
            }
        };

        return Task.FromResult(response);
    }

    public Task<Intent> AnalyzeIntentAsync(string message, CancellationToken cancellationToken = default)
    {
        var text = (message ?? string.Empty).ToLowerInvariant();
        Intent intent;

        if (text.Contains("reunión") || text.Contains("cita") || text.Contains("agendar") || text.Contains("recordatorio"))
        {
            intent = new Intent(Intent.CreateReminder, 0.95f, new Dictionary<string, string> { ["Action"] = "CreateCalendarEvent" });
        }
        else if (text.Contains("correo") || text.Contains("email") || text.Contains("enviar"))
        {
            intent = new Intent("SendEmail", 0.92f, new Dictionary<string, string> { ["Action"] = "SendEmail" });
        }
        else if (text.Contains("hoja") || text.Contains("excel") || text.Contains("sheet") || text.Contains("tabla"))
        {
            intent = new Intent("AppendSpreadsheetRow", 0.90f, new Dictionary<string, string> { ["Action"] = "AppendSpreadsheetRow" });
        }
        else if (text.Contains("hola") || text.Contains("buenos días"))
        {
            intent = new Intent(Intent.Greeting, 0.99f);
        }
        else
        {
            intent = new Intent(Intent.GetInformation, 0.80f);
        }

        return Task.FromResult(intent);
    }

    public Task<T?> ExtractStructuredDataAsync<T>(string text, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = JsonSerializer.Deserialize<T>(text);
            return Task.FromResult(result);
        }
        catch
        {
            return Task.FromResult<T?>(default);
        }
    }

    public AiModelInformation GetModelInformation()
    {
        return new AiModelInformation
        {
            Provider = "MockAI",
            Model = "mock-gpt-4o",
            ContextSize = 8192,
            Capabilities = new List<string> { "text-generation", "intent-analysis", "structured-data" }
        };
    }
}
