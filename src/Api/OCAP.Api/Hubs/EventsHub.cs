using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace OCAP.Api.Hubs;

// SignalR Hub para la transmisión de eventos en vivo de la plataforma OCAP con aislamiento Multi-Tenant.
[Authorize]
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
            ?? Context.User?.FindFirst("TenantId")?.Value;

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

    // La suscripción explícita solo admite el tenant autenticado en el JWT.
    public async Task SubscribeTenant(string tenantId)
    {
        var authenticatedTenant = RequireAuthenticatedTenant();
        if (!Guid.TryParse(tenantId, out var requestedTenant)
            || requestedTenant != authenticatedTenant)
            throw new HubException("No se permite suscribirse a otro tenant.");

        var groupName = GetTenantGroupName(authenticatedTenant);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        _logger?.LogInformation("Cliente {ConnectionId} se suscribió al grupo del Tenant {TenantId}", Context.ConnectionId, authenticatedTenant);
    }

    public async Task UnsubscribeTenant(string tenantId)
    {
        var authenticatedTenant = RequireAuthenticatedTenant();
        if (!Guid.TryParse(tenantId, out var requestedTenant)
            || requestedTenant != authenticatedTenant)
            throw new HubException("No se permite desuscribirse de otro tenant.");

        var groupName = GetTenantGroupName(authenticatedTenant);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        _logger?.LogInformation("Cliente {ConnectionId} se desuscribió del grupo del Tenant {TenantId}", Context.ConnectionId, authenticatedTenant);
    }

    public static string GetTenantGroupName(Guid tenantId) => $"tenant_{tenantId}";

    private Guid RequireAuthenticatedTenant()
    {
        var claim = Context.User?.FindFirst("tenant_id")?.Value
            ?? Context.User?.FindFirst("TenantId")?.Value;

        if (!Guid.TryParse(claim, out var tenantId) || tenantId == Guid.Empty)
            throw new HubException("El token no contiene un tenant válido.");

        return tenantId;
    }
}
