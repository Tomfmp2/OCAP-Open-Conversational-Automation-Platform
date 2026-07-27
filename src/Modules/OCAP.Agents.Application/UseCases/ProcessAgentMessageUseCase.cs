using Microsoft.Extensions.Logging;
using OCAP.Agents.Abstractions.Ports;
using OCAP.Agents.Domain.Entities;
using OCAP.Agents.Domain.ValueObjects;

namespace OCAP.Agents.Application.UseCases;

// Caso de uso principal que orquesta la inteligencia conversacional del Agent Engine.
// Recibe un mensaje entrante, identifica el agente, resuelve la intención, actualiza el contexto y determina la respuesta/acción.
public class ProcessAgentMessageUseCase
{
    private readonly IAgentRepository _agentRepository;
    private readonly IConversationContextRepository _contextRepository;
    private readonly IIntentResolver _intentResolver;
    private readonly IActionDispatcher _actionDispatcher;
    private readonly ILogger<ProcessAgentMessageUseCase> _logger;

    public ProcessAgentMessageUseCase(
        IAgentRepository agentRepository,
        IConversationContextRepository contextRepository,
        IIntentResolver intentResolver,
        IActionDispatcher actionDispatcher,
        ILogger<ProcessAgentMessageUseCase> logger)
    {
        _agentRepository = agentRepository;
        _contextRepository = contextRepository;
        _intentResolver = intentResolver;
        _actionDispatcher = actionDispatcher;
        _logger = logger;
    }

    // Orquesta la respuesta conversacional completa para una conversación.
    public async Task<string> ExecuteAsync(Guid conversationId, string userMessage, CancellationToken cancellationToken = default)
    {
        if (conversationId == Guid.Empty) throw new ArgumentException("El ID de conversación no puede ser vacío.", nameof(conversationId));
        if (string.IsNullOrWhiteSpace(userMessage)) return "Hola, ¿en qué puedo ayudarte hoy?";

        _logger.LogInformation("Orquestando mensaje conversacional para conversación {ConversationId}", conversationId);

        // 1. Obtener o crear el agente por defecto
        var agent = await _agentRepository.GetDefaultAgentAsync(cancellationToken);
        if (agent == null)
        {
            var defaultConfig = new AgentConfiguration("Eres el asistente virtual por defecto de OCAP.");
            agent = new Agent(Guid.NewGuid(), new AgentName("Asistente OCAP"), "Agente conversacional principal", defaultConfig);
            await _agentRepository.SaveAsync(agent, cancellationToken);
        }

        if (agent.Status != AgentStatus.Active)
        {
            return "El asistente se encuentra temporalmente fuera de servicio por mantenimiento.";
        }

        // 2. Obtener o crear contexto conversacional
        var context = await _contextRepository.GetByConversationIdAsync(conversationId, cancellationToken)
            ?? new ConversationContext(conversationId);

        // 3. Resolver la intención del mensaje
        var intent = await _intentResolver.ResolveIntentAsync(userMessage, context, cancellationToken);
        _logger.LogInformation("Intención resuelta: {IntentName} (Confianza: {Confidence})", intent.Name, intent.Confidence);

        // 4. Actualizar el contexto conversacional
        context.SetIntent(intent.Name);
        foreach (var param in intent.Parameters)
        {
            context.SetParameter(param.Key, param.Value);
        }
        await _contextRepository.SaveAsync(context, cancellationToken);

        // 5. Determinar acción y generar respuesta según la intención
        return intent.Name switch
        {
            Intent.Greeting => "¡Hola! Soy OCAP. ¿En qué puedo colaborar contigo hoy?",
            Intent.CreateReminder => "He registrado tu solicitud de recordatorio. Próximamente me integraré con tu calendario.",
            Intent.HumanSupport => "Entendido. Un asesor del equipo humano tomará el control de esta conversación en breve.",
            Intent.GetInformation => "OCAP (Open Conversational Automation Platform) es una plataforma open source para automatización de diálogos.",
            _ => $"Comprendo tu mensaje ('{userMessage}'). Actualmente estoy aprendiendo a procesar esta solicitud."
        };
    }
}
