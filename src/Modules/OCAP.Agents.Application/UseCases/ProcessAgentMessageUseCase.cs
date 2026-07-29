using Microsoft.Extensions.Logging;
using OCAP.Agents.Abstractions.Contracts;
using OCAP.Agents.Abstractions.Models;
using OCAP.Agents.Abstractions.Ports;
using OCAP.Agents.Domain.Entities;
using OCAP.Agents.Domain.ValueObjects;

namespace OCAP.Agents.Application.UseCases;

// Caso de uso principal que orquesta el flujo de ejecución conversacional de OCAP:
// Channel Adapter → Application Use Case → Identity Resolution → Agent Runtime → AI Provider → Response Pipeline → Channel Adapter
public class ProcessAgentMessageUseCase
{
    private readonly IAgentRepository _agentRepository;
    private readonly IConversationContextRepository _contextRepository;
    private readonly IIntentResolver _intentResolver;
    private readonly IActionDispatcher _actionDispatcher;
    private readonly IAgentResolver? _agentResolver;
    private readonly IAgentRuntime? _agentRuntime;
    private readonly OCAP.Core.Events.IEventBus? _eventBus;
    private readonly ILogger<ProcessAgentMessageUseCase> _logger;

    public ProcessAgentMessageUseCase(
        IAgentRepository agentRepository,
        IConversationContextRepository contextRepository,
        IIntentResolver intentResolver,
        IActionDispatcher actionDispatcher,
        ILogger<ProcessAgentMessageUseCase> logger,
        IAgentResolver? agentResolver = null,
        IAgentRuntime? agentRuntime = null,
        OCAP.Core.Events.IEventBus? eventBus = null)
    {
        _agentRepository = agentRepository;
        _contextRepository = contextRepository;
        _intentResolver = intentResolver;
        _actionDispatcher = actionDispatcher;
        _logger = logger;
        _agentResolver = agentResolver;
        _agentRuntime = agentRuntime;
        _eventBus = eventBus;
    }

    public async Task<string> ExecuteAsync(Guid conversationId, string userMessage, CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync(conversationId, userMessage, Guid.NewGuid(), Guid.NewGuid(), cancellationToken);
    }

    public async Task<string> ExecuteAsync(
        Guid conversationId,
        string userMessage,
        Guid tenantId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (conversationId == Guid.Empty) throw new ArgumentException("El ID de conversación no puede ser vacío.", nameof(conversationId));
        if (string.IsNullOrWhiteSpace(userMessage)) return "Hola, ¿en qué puedo ayudarte hoy?";

        _logger.LogInformation("Orquestando mensaje conversacional para conversación {ConversationId}, Tenant {TenantId}, User {UserId}",
            conversationId, tenantId, userId);

        // Si AgentRuntime y AgentResolver están registrados, delegar al Agent Runtime Pipeline
        if (_agentResolver != null && _agentRuntime != null)
        {
            var resolvedAgentId = await _agentResolver.ResolveAgentIdAsync(tenantId, userId, userMessage, cancellationToken);
            var agentContext = new AgentContext(resolvedAgentId, tenantId, userId, userMessage);

            return await _agentRuntime.ExecuteAgentAsync(agentContext, cancellationToken);
        }

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

        // 5. Determinar acción y ejecutar según la intención
        if (intent.Name == Intent.CreateReminder)
        {
            var actionParams = new Dictionary<string, object>
            {
                ["Title"] = "Recordatorio solicitado por usuario",
                ["Description"] = userMessage,
                ["StartDate"] = DateTime.UtcNow.AddDays(1)
            };
            var action = new AgentAction(AgentAction.CreateCalendarEvent, "CreateCalendarEventTool", actionParams);
            var result = await _actionDispatcher.DispatchActionAsync(agent.Id, Guid.Empty, conversationId, action, cancellationToken);

            if (result.Success)
            {
                return $"¡Recordatorio registrado! {result.Message}";
            }
            return $"No fue posible registrar el recordatorio: {result.Message} (Error: {result.ErrorCode})";
        }

        return intent.Name switch
        {
            Intent.Greeting => "¡Hola! Soy OCAP. ¿En qué puedo colaborar contigo hoy?",
            Intent.HumanSupport => "Entendido. Un asesor del equipo humano tomará el control de esta conversación en breve.",
            Intent.GetInformation => "OCAP (Open Conversational Automation Platform) es una plataforma open source para automatización de diálogos.",
            _ => $"Comprendo tu mensaje ('{userMessage}'). Actualmente estoy aprendiendo a procesar esta solicitud."
        };
    }
}
