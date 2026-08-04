using OCAP.Channels.Abstractions.Contracts;
using OCAP.Security.Domain.Entities;

namespace OCAP.Channels.WebChat.Services;

public interface IWebChatRuntimeManager
{
    Task<ChannelConnection> RegisterConnectionAsync(
        Guid tenantId,
        string displayName,
        string? widgetTitle,
        CancellationToken cancellationToken = default);

    Task<object> GetHealthAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

public class WebChatRuntimeManager : IWebChatRuntimeManager
{
    private readonly IChannelConnectionManager _connectionManager;

    public WebChatRuntimeManager(IChannelConnectionManager connectionManager)
    {
        _connectionManager = connectionManager;
    }

    public async Task<ChannelConnection> RegisterConnectionAsync(
        Guid tenantId,
        string displayName,
        string? widgetTitle,
        CancellationToken cancellationToken = default)
    {
        var metadata = new Dictionary<string, string>
        {
            ["WidgetTitle"] = string.IsNullOrWhiteSpace(widgetTitle) ? "Asistente OCAP" : widgetTitle.Trim()
        };

        // Credencial opaca de widget (no es un secreto de proveedor externo).
        var widgetKey = Convert.ToBase64String(Guid.NewGuid().ToByteArray());

        return await _connectionManager.CreateConnectionAsync(
            tenantId,
            "WebChat",
            displayName,
            widgetKey,
            metadata,
            cancellationToken);
    }

    public async Task<object> GetHealthAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var connections = await _connectionManager.GetTenantConnectionsAsync(tenantId, cancellationToken);
        var webChat = connections.FirstOrDefault(c =>
            string.Equals(c.Provider, "WebChat", StringComparison.OrdinalIgnoreCase));

        return new
        {
            Provider = "WebChat",
            Health = webChat?.Enabled == true ? "Healthy" : "Disconnected",
            LatencyMs = 1,
            LastPingAtUtc = DateTime.UtcNow,
            ConnectionId = webChat?.Id,
            Enabled = webChat?.Enabled == true
        };
    }
}
