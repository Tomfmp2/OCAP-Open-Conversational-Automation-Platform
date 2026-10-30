namespace OCAP.Agents.Application.Services;

/// <summary>
/// Herramientas que el canal concede en la petición en curso (scoped). Cuando un canal define las suyas, mandan sobre
/// la lista del agente: el mismo agente puede tener acciones distintas en cada canal.
/// </summary>
public sealed class ChannelToolGrants
{
    public IReadOnlySet<string>? Granted { get; private set; }

    public void Grant(IEnumerable<string> toolNames) => Granted = new HashSet<string>(toolNames, StringComparer.OrdinalIgnoreCase);
}
