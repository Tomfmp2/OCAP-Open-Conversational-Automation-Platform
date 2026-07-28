using Microsoft.Extensions.Logging;
using OCAP.Agents.Abstractions.Contracts;
using OCAP.Agents.Abstractions.Models;
using OCAP.Agents.Abstractions.Providers;

namespace OCAP.Agents.Application.Services;

// Implementación del Enterprise Assistant Agent de OCAP.
// Actúa como orquestador principal independiente de canales, infraestructura, bases de datos o proveedores de IA específicos.
public class EnterpriseAssistantAgent : IEnterpriseAssistantAgent
{
    public static readonly Guid EnterpriseAssistantAgentId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    public Guid GlobalAgentId => EnterpriseAssistantAgentId;

    private readonly ILanguageModelProviderSelector _providerSelector;
    private readonly ILogger<EnterpriseAssistantAgent> _logger;

    public EnterpriseAssistantAgent(
        ILanguageModelProviderSelector providerSelector,
        ILogger<EnterpriseAssistantAgent> logger)
    {
        _providerSelector = providerSelector;
        _logger = logger;
    }

    public async Task<AgentExecutionResult> ProcessRequestAsync(IAgentContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        _logger.LogInformation("Enterprise Assistant Agent procesando solicitud para Tenant {TenantId}, User {UserId}",
            context.TenantId, context.UserId);

        var provider = await _providerSelector.GetProviderAsync(context.TenantId, null, cancellationToken);

        var systemPrompt = PromptMessage.System(
            "Eres el Enterprise Assistant Agent oficial de OCAP (Open Conversational Automation Platform). " +
            "Actúas como agente global orquestador encargado de comprender usuarios, coordinar capacidades y brindar respuestas precisas y profesionales.");

        var userPrompt = PromptMessage.User(context.UserMessage);

        var request = new LanguageModelRequest(new[] { systemPrompt, userPrompt });

        var response = await provider.GenerateAsync(request, cancellationToken);

        return AgentExecutionResult.CreateSuccess(
            response.Content,
            GlobalAgentId,
            response.ProviderName,
            new Dictionary<string, object>
            {
                ["TokensUsed"] = response.TokensUsed,
                ["ModelUsed"] = response.ModelUsed
            });
    }
}
