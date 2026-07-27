using OCAP.Dashboard.Models;

namespace OCAP.Dashboard.State;

// Contenedor de estado reactivo global para el Dashboard.
public class DashboardStateContainer
{
    public AgentModel? SelectedAgent { get; private set; }
    public ConversationModel? SelectedConversation { get; private set; }

    public event Action? OnStateChanged;

    public void SelectAgent(AgentModel agent)
    {
        SelectedAgent = agent;
        NotifyStateChanged();
    }

    public void SelectConversation(ConversationModel conversation)
    {
        SelectedConversation = conversation;
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnStateChanged?.Invoke();
}
