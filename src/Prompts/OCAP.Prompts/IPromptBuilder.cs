using OCAP.Agents.Domain.Entities;
using OCAP.Tools.Abstractions;

namespace OCAP.Prompts;

// Contrato para la construcción dinámica de prompts contextuales orientados a agentes de OCAP.
public interface IPromptBuilder
{
    // Construye un PromptTemplate dinámico utilizando la configuración del agente, mensaje y herramientas.
    PromptTemplate BuildPrompt(Agent agent, string userMessage, ConversationContext? context, IReadOnlyCollection<ITool>? availableTools);
    PromptTemplate BuildPromptWithKnowledge(Agent agent, string userMessage, ConversationContext? context, IReadOnlyCollection<ITool>? availableTools, IReadOnlyCollection<string>? knowledgeSnippets);
}
