using OCAP.Tools.Abstractions;

namespace OCAP.Security.Abstractions;

// Validador de permisos predeterminado para evaluar las políticas asociadas a agentes.
public class DefaultPermissionValidator : IPermissionValidator
{
    private readonly Dictionary<Guid, AgentPermissionPolicy> _policies = new();

    // Registra o actualiza la política de permisos de un agente.
    public void SetPolicy(AgentPermissionPolicy policy)
    {
        if (policy == null) throw new ArgumentNullException(nameof(policy));
        _policies[policy.AgentId] = policy;
    }

    public Task<bool> HasPermissionAsync(Guid agentId, string permission, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(permission)) return Task.FromResult(false);

        if (_policies.TryGetValue(agentId, out var policy))
        {
            return Task.FromResult(policy.IsPermissionAllowed(permission));
        }

        // Por defecto deny-all: sin política explícita no se conceden permisos.
        return Task.FromResult(false);
    }

    public async Task<bool> CanExecuteToolAsync(Guid agentId, ITool tool, CancellationToken cancellationToken = default)
    {
        if (tool == null) return false;

        var requiredPermissions = tool.Definition.RequiredPermissions;
        if (requiredPermissions == null || requiredPermissions.Count == 0)
        {
            return true;
        }

        foreach (var permission in requiredPermissions)
        {
            var hasPerm = await HasPermissionAsync(agentId, permission, cancellationToken);
            if (!hasPerm) return false;
        }

        return true;
    }
}
