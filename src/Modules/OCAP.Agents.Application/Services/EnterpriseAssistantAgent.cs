using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OCAP.Agents.Abstractions.Contracts;
using OCAP.Agents.Abstractions.Models;
using OCAP.Agents.Abstractions.Ports;
using OCAP.Agents.Abstractions.Providers;
using OCAP.Agents.Domain.Entities;
using OCAP.Tools.Abstractions;

namespace OCAP.Agents.Application.Services;

/// <summary>
/// Agente madre / orquestador global de OCAP.
/// Usa un snapshot real del tenant + herramientas Google Workspace.
/// </summary>
public class EnterpriseAssistantAgent : IEnterpriseAssistantAgent
{
    public static readonly Guid EnterpriseAssistantAgentId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    public Guid GlobalAgentId => EnterpriseAssistantAgentId;

    private readonly ILanguageModelProviderSelector _providerSelector;
    private readonly IToolRegistry _toolRegistry;
    private readonly IActionDispatcher _actionDispatcher;
    private readonly IOcapSystemContextProvider _systemContext;
    private readonly IConfiguration _configuration;
    private readonly ILogger<EnterpriseAssistantAgent> _logger;

    public EnterpriseAssistantAgent(
        ILanguageModelProviderSelector providerSelector,
        IToolRegistry toolRegistry,
        IActionDispatcher actionDispatcher,
        IOcapSystemContextProvider systemContext,
        IConfiguration configuration,
        ILogger<EnterpriseAssistantAgent> logger)
    {
        _providerSelector = providerSelector;
        _toolRegistry = toolRegistry;
        _actionDispatcher = actionDispatcher;
        _systemContext = systemContext;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<AgentExecutionResult> ProcessRequestAsync(IAgentContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        _logger.LogInformation(
            "Agente madre procesando Tenant {TenantId}, User {UserId}",
            context.TenantId,
            context.UserId);

        var preferred = _configuration["AiProviders:PreferredProvider"];
        var provider = await _providerSelector.GetProviderAsync(context.TenantId, preferred, cancellationToken);
        var toolsCatalog = BuildToolsCatalog();
        var snapshot = await _systemContext.GetTenantSnapshotAsync(context.TenantId, cancellationToken);

        var systemPrompt = PromptMessage.System(
            """
            Eres el Agente Principal (madre) de OCAP (Open Conversational Automation Platform).
            Tienes acceso al ESTADO REAL del tenant del usuario (más abajo). Debes usarlo como fuente de verdad.

            Reglas obligatorias:
            - Responde en español, claro y concreto.
            - Cuando pregunten por canales, agentes, IA, conocimiento o usuarios: responde con los datos del ESTADO REAL (nombres, providers, enabled, conteos). NO digas solo "ve a la sección Canales".
            - Si un dato no aparece en el ESTADO REAL, dilo explícitamente ("no hay registros") en lugar de inventar.
            - Puedes añadir una frase corta de cómo gestionar algo en el panel, pero primero da los hechos.
            - Si el usuario pide enviar un correo, crear un evento o escribir en una hoja, DEBES usar una herramienta.
            - Cuando necesites una herramienta, responde ÚNICAMENTE con un JSON válido (sin markdown):
              {"reply":"texto breve al usuario","tool":"NombreExactoTool","args":{...}}
            - Si no necesitas herramienta: {"reply":"tu respuesta con datos reales","tool":null,"args":{}}
            - Tools disponibles:
            """ + toolsCatalog + """

            Ejemplos de args:
            - SendEmailTool: {"To":"user@dominio.com","Subject":"Asunto","Body":"Cuerpo"}
            - CreateCalendarEventTool: {"Title":"...","Description":"...","StartDate":"2026-08-05T15:00:00Z"}
            - ListEmailsTool: {"MaxResults":5}

            """ + snapshot);

        var userPrompt = PromptMessage.User(context.UserMessage);
        var request = new LanguageModelRequest(new[] { systemPrompt, userPrompt });
        var response = await provider.GenerateAsync(request, cancellationToken);

        var parsed = TryParseAssistantPlan(response.Content);
        var reply = parsed?.Reply ?? response.Content;
        var metadata = new Dictionary<string, object>
        {
            ["TokensUsed"] = response.TokensUsed,
            ["ModelUsed"] = response.ModelUsed,
            ["HasSystemSnapshot"] = true
        };

        if (parsed != null &&
            !string.IsNullOrWhiteSpace(parsed.Tool) &&
            !string.Equals(parsed.Tool, "null", StringComparison.OrdinalIgnoreCase))
        {
            var toolName = parsed.Tool.Trim();
            var actionType = toolName.Contains("Email", StringComparison.OrdinalIgnoreCase)
                ? AgentAction.SendEmail
                : toolName.Contains("Calendar", StringComparison.OrdinalIgnoreCase)
                    ? AgentAction.CreateCalendarEvent
                    : AgentAction.GenerateResponse;

            var action = new AgentAction(actionType, toolName, parsed.Args);
            var conversationId = context.AgentId;
            var toolResult = await _actionDispatcher.DispatchActionAsync(
                GlobalAgentId,
                context.UserId,
                conversationId,
                action,
                cancellationToken);

            metadata["Tool"] = toolName;
            metadata["ToolSuccess"] = toolResult.Success;
            metadata["ToolMessage"] = toolResult.Message ?? string.Empty;

            if (toolResult.Success)
            {
                reply = string.IsNullOrWhiteSpace(parsed.Reply)
                    ? $"Listo. {toolResult.Message}"
                    : $"{parsed.Reply}\n\n✓ {toolResult.Message}";
            }
            else
            {
                reply = string.IsNullOrWhiteSpace(parsed.Reply)
                    ? $"No pude completar la acción: {toolResult.Message}"
                    : $"{parsed.Reply}\n\n✗ {toolResult.Message}";
            }
        }

        return AgentExecutionResult.CreateSuccess(reply, GlobalAgentId, response.ProviderName, metadata);
    }

    private string BuildToolsCatalog()
    {
        var tools = _toolRegistry.GetAllTools();
        if (tools.Count == 0)
        {
            return "(ninguna herramienta registrada en este entorno)";
        }

        return string.Join(
            "\n",
            tools.Select(t =>
                $"- {t.Definition.Name}: {t.Definition.Description} | input {t.Definition.InputSchema}"));
    }

    private static AssistantPlan? TryParseAssistantPlan(string? content)
    {
        if (string.IsNullOrWhiteSpace(content)) return null;

        var text = content.Trim();
        if (text.StartsWith("```", StringComparison.Ordinal))
        {
            var start = text.IndexOf('{');
            var end = text.LastIndexOf('}');
            if (start >= 0 && end > start)
            {
                text = text[start..(end + 1)];
            }
        }

        var jsonStart = text.IndexOf('{');
        var jsonEnd = text.LastIndexOf('}');
        if (jsonStart < 0 || jsonEnd <= jsonStart)
        {
            return null;
        }

        text = text[jsonStart..(jsonEnd + 1)];

        try
        {
            using var doc = JsonDocument.Parse(text);
            var root = doc.RootElement;
            var reply = root.TryGetProperty("reply", out var r) ? r.GetString() ?? string.Empty : string.Empty;
            string? tool = null;
            if (root.TryGetProperty("tool", out var t))
            {
                tool = t.ValueKind == JsonValueKind.Null ? null : t.GetString();
            }

            var args = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            if (root.TryGetProperty("args", out var a) && a.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in a.EnumerateObject())
                {
                    args[prop.Name] = ConvertJsonElement(prop.Value);
                }
            }

            return new AssistantPlan(reply, tool, args);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static object ConvertJsonElement(JsonElement el) =>
        el.ValueKind switch
        {
            JsonValueKind.String => el.GetString() ?? string.Empty,
            JsonValueKind.Number => el.TryGetInt64(out var l) ? l : el.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Array => el.EnumerateArray().Select(ConvertJsonElement).ToList(),
            JsonValueKind.Object => el.EnumerateObject()
                .ToDictionary(p => p.Name, p => ConvertJsonElement(p.Value), StringComparer.OrdinalIgnoreCase),
            _ => el.ToString()
        };

    private sealed record AssistantPlan(string Reply, string? Tool, Dictionary<string, object> Args);
}
