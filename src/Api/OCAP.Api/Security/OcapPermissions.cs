using Microsoft.AspNetCore.Authorization;

namespace OCAP.Api.Security;

/// <summary>
/// Permisos del rol (claim <c>permission</c> del JWT) que exige cada área de la API. Cada código es también el nombre
/// de su política: <c>[Authorize(Policy = OcapPermissions.ChannelManage)]</c>. <see cref="FullAccess"/> los cumple todos.
/// </summary>
public static class OcapPermissions
{
    public const string ClaimType = "permission";
    public const string FullAccess = "Admin.Full";

    public const string ConversationRead = "Conversation.Read";
    public const string ConversationWrite = "Conversation.Write";
    public const string AgentRead = "Agent.Read";
    public const string AgentWrite = "Agent.Write";
    public const string AgentExecute = "Agent.Execute";
    public const string ToolExecute = "Tool.Execute";
    public const string DashboardRead = "Dashboard.Read";
    public const string DeploymentManage = "Deployment.Manage";
    public const string AiExecute = "AI.Execute";
    public const string SettingsManage = "Settings.Manage";
    public const string OAuthManage = "OAuth.Manage";
    public const string KnowledgeManage = "Knowledge.Manage";
    public const string WorkflowManage = "Workflow.Manage";
    public const string SecurityManage = "Security.Manage";
    public const string ChannelManage = "Channel.Manage";

    /// <summary>Todos los permisos: los tiene el rol Admin.</summary>
    public static readonly IReadOnlyList<string> All =
    [
        ConversationRead, ConversationWrite, AgentRead, AgentWrite, AgentExecute, ToolExecute, DashboardRead,
        DeploymentManage, AiExecute, SettingsManage, OAuthManage, KnowledgeManage, WorkflowManage, SecurityManage,
        ChannelManage
    ];

    /// <summary>Lo que necesita quien atiende la bandeja y usa el asistente, sin administrar la organización.</summary>
    public static readonly IReadOnlyList<string> Operator =
    [
        ConversationRead, ConversationWrite, AgentRead, AgentExecute, DashboardRead, AiExecute
    ];

    public static AuthorizationOptions AddOcapPermissionPolicies(this AuthorizationOptions options)
    {
        foreach (var permission in All)
        {
            options.AddPolicy(permission, policy => policy
                .RequireAuthenticatedUser()
                .RequireAssertion(context => context.User.HasClaim(c =>
                    c.Type == ClaimType && (c.Value == permission || c.Value == FullAccess))));
        }

        options.AddPolicy(FullAccess, policy => policy.RequireAuthenticatedUser().RequireClaim(ClaimType, FullAccess));

        return options;
    }
}
