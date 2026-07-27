using System.Text;
using OCAP.Agents.Domain.Entities;
using OCAP.Tools.Abstractions;

namespace OCAP.Prompts;

// Generador de prompts del sistema que inyecta contexto, rol del agente y herramientas disponibles.
public class SystemPromptBuilder : IPromptBuilder
{
    public PromptTemplate BuildPrompt(Agent agent, string userMessage, ConversationContext? context, IReadOnlyCollection<ITool>? availableTools)
    {
        if (agent == null) throw new ArgumentNullException(nameof(agent));

        var sbTools = new StringBuilder();
        if (availableTools != null && availableTools.Any())
        {
            sbTools.AppendLine("Herramientas ejecutables disponibles:");
            foreach (var tool in availableTools)
            {
                sbTools.AppendLine($"- {tool.Definition.Name}: {tool.Definition.Description} (Permisos: {string.Join(", ", tool.Definition.RequiredPermissions)})");
            }
        }
        else
        {
            sbTools.AppendLine("No hay herramientas externas configuradas para este agente.");
        }

        var systemPromptText = $"{agent.Configuration.SystemPrompt}\n\n{sbTools}";

        var template = new PromptTemplate
        {
            Name = $"{agent.Name}_Prompt",
            Version = "1.0.0",
            SystemPrompt = systemPromptText,
            UserPrompt = userMessage ?? string.Empty
        };

        if (context != null)
        {
            template.DynamicVariables["CurrentIntent"] = context.CurrentIntent ?? "None";
            template.DynamicVariables["LastInteraction"] = context.LastInteractionAt.ToString("u");
        }

        return template;
    }
}
