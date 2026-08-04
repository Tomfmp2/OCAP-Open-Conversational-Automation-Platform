using Microsoft.Extensions.Logging;
using OCAP.Agents.Abstractions.Ports;
using OCAP.Agents.Domain.Entities;
using OCAP.Agents.Domain.ValueObjects;
using OCAP.Intelligence.Abstractions;
using OCAP.Prompts;
using OCAP.Tools.Abstractions;

namespace OCAP.Intelligence.Application.Services;

// Implementación del motor de razonamiento de agentes que coordina IA, contexto y herramientas.
public class AgentReasoningService : IAgentReasoningService
{
    private readonly IAgentRepository _agentRepository;
    private readonly IToolRegistry _toolRegistry;
    private readonly IPromptBuilder _promptBuilder;
    private readonly IAiProvider _aiProvider;
    private readonly IActionDispatcher _actionDispatcher;
    private readonly IAiUsageTracker? _usageTracker;
    private readonly ILogger<AgentReasoningService> _logger;

    public AgentReasoningService(
        IAgentRepository agentRepository,
        IToolRegistry toolRegistry,
        IPromptBuilder promptBuilder,
        IAiProvider aiProvider,
        IActionDispatcher actionDispatcher,
        IAiUsageTracker? usageTracker,
        ILogger<AgentReasoningService> logger)
    {
        _agentRepository = agentRepository ?? throw new ArgumentNullException(nameof(agentRepository));
        _toolRegistry = toolRegistry ?? throw new ArgumentNullException(nameof(toolRegistry));
        _promptBuilder = promptBuilder ?? throw new ArgumentNullException(nameof(promptBuilder));
        _aiProvider = aiProvider ?? throw new ArgumentNullException(nameof(aiProvider));
        _actionDispatcher = actionDispatcher ?? throw new ArgumentNullException(nameof(actionDispatcher));
        _usageTracker = usageTracker;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string> ProcessReasoningAsync(Guid agentId, Guid userId, Guid conversationId, string userMessage, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userMessage)) return "Hola, ¿en qué puedo ayudarte hoy?";

        _logger.LogInformation("Iniciando ciclo de razonamiento IA para agente {AgentId} y conversación {ConversationId}", agentId, conversationId);

        // 1. Cargar agente o agente por defecto
        var agent = await _agentRepository.GetByIdAsync(agentId, cancellationToken)
            ?? await _agentRepository.GetDefaultAgentAsync(cancellationToken);

        if (agent == null)
        {
            var defaultConfig = new AgentConfiguration("Eres el asistente IA principal de OCAP.");
            agent = new Agent(agentId != Guid.Empty ? agentId : Guid.NewGuid(), new AgentName("Asistente IA OCAP"), "Agente conversacional de inteligencia generativa", defaultConfig);
        }

        // 2. Obtener herramientas registradas en el sistema
        var availableTools = _toolRegistry.GetAllTools();

        // 3. Construir el prompt dinámico
        var promptTemplate = _promptBuilder.BuildPrompt(agent, userMessage, null, availableTools);

        // 4. Crear la solicitud para el proveedor de IA
        var aiRequest = new AiRequest
        {
            AgentId = agent.Id,
            ConversationId = conversationId,
            UserMessage = promptTemplate.RenderUserPrompt(),
            SystemInstructions = promptTemplate.RenderSystemPrompt()
        };

        // 5. Consultar al proveedor de IA
        var aiResponse = await _aiProvider.GenerateResponseAsync(aiRequest, cancellationToken);

        // 6. Analizar la intención conversacional
        var intent = await _aiProvider.AnalyzeIntentAsync(userMessage, cancellationToken);
        _logger.LogInformation("Intención IA analizada: {IntentName} con confianza {Confidence}", intent.Name, intent.Confidence);

        string responseText = aiResponse.GeneratedText;

        // 7. Ejecución de herramientas según la intención detectada
        if (intent.Name == Intent.CreateReminder)
        {
            var actionParams = new Dictionary<string, object>
            {
                ["Title"] = "Reunión agendada vía IA",
                ["Description"] = userMessage,
                ["StartDate"] = DateTime.UtcNow.AddDays(1)
            };
            var action = new AgentAction(AgentAction.CreateCalendarEvent, "CreateCalendarEventTool", actionParams);
            var dispatchResult = await _actionDispatcher.DispatchActionAsync(agent.Id, userId, conversationId, action, cancellationToken);

            if (dispatchResult.Success)
            {
                responseText += $"\n\n[Acción Ejecutada]: {dispatchResult.Message}";
            }
            else
            {
                responseText += $"\n\n[Error al ejecutar herramienta]: {dispatchResult.Message}";
            }
        }
        else if (intent.Name == Intent.SendEmail || intent.Name == "SendEmail")
        {
            var to = intent.Parameters.TryGetValue("To", out var toVal) ? toVal : string.Empty;
            var subject = intent.Parameters.TryGetValue("Subject", out var subVal) ? subVal : "Mensaje desde OCAP";
            var body = intent.Parameters.TryGetValue("Body", out var bodyVal) ? bodyVal : userMessage;
            if (!string.IsNullOrWhiteSpace(to))
            {
                var action = new AgentAction(AgentAction.SendEmail, "SendEmailTool", new Dictionary<string, object>
                {
                    ["To"] = to,
                    ["Subject"] = subject,
                    ["Body"] = body
                });
                var dispatchResult = await _actionDispatcher.DispatchActionAsync(agent.Id, userId, conversationId, action, cancellationToken);
                responseText += dispatchResult.Success
                    ? $"\n\n[Correo]: {dispatchResult.Message}"
                    : $"\n\n[Error correo]: {dispatchResult.Message}";
            }
        }

        // 8. Registrar métricas de uso de IA si el tracker está configurado
        if (_usageTracker != null)
        {
            await _usageTracker.TrackUsageAsync(userId, agent.Id, aiResponse.ProviderName, aiResponse.ModelName, aiResponse.TokensUsed, true, cancellationToken);
        }

        return responseText;
    }
}
