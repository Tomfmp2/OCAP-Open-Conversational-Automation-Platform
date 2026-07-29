using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace OCAP.Api.Hubs;

// SignalR Hub para la transmisión de eventos en vivo de la plataforma OCAP con aislamiento Multi-Tenant.
public class EventsHub : Hub
{
    private readonly ILogger<EventsHub>? _logger;

    public EventsHub(ILogger<EventsHub>? logger = null)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var tenantIdClaim = Context.User?.FindFirst("tenant_id")?.Value
            ?? Context.GetHttpContext()?.Request.Query["tenantId"].ToString();

        if (!string.IsNullOrWhiteSpace(tenantIdClaim) && Guid.TryParse(tenantIdClaim, out var tenantId))
        {
            var groupName = GetTenantGroupName(tenantId);
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            _logger?.LogInformation("Cliente {ConnectionId} conectado al grupo SignalR del Tenant {TenantId}", Context.ConnectionId, tenantId);
        }
        else
        {
            _logger?.LogInformation("Cliente {ConnectionId} conectado a EventsHub sin TenantId inicial", Context.ConnectionId);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger?.LogInformation("Cliente {ConnectionId} desconectado de EventsHub", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    // Permite a los clientes suscribirse explícitamente al grupo de su TenantId
    public async Task SubscribeTenant(string tenantId)
    {
        if (Guid.TryParse(tenantId, out var tenantGuid))
        {
            var groupName = GetTenantGroupName(tenantGuid);
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            _logger?.LogInformation("Cliente {ConnectionId} se suscribió al grupo del Tenant {TenantId}", Context.ConnectionId, tenantGuid);
        }
    }

    public async Task UnsubscribeTenant(string tenantId)
    {
        if (Guid.TryParse(tenantId, out var tenantGuid))
        {
            var groupName = GetTenantGroupName(tenantGuid);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
            _logger?.LogInformation("Cliente {ConnectionId} se desuscribió del grupo del Tenant {TenantId}", Context.ConnectionId, tenantGuid);
        }
    }

    public static string GetTenantGroupName(Guid tenantId) => $"tenant_{tenantId}";
}
