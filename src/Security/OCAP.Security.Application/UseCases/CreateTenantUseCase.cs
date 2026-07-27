using OCAP.Security.Abstractions;
using OCAP.Security.Domain.Entities;

namespace OCAP.Security.Application.UseCases;

// Caso de uso para la creación e inicialización de un nuevo Tenant u Organización.
public class CreateTenantUseCase
{
    private readonly ISecurityAuditService _auditService;

    public CreateTenantUseCase(ISecurityAuditService auditService)
    {
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
    }

    public async Task<Tenant> ExecuteAsync(string name, string slug, Guid ownerUserId, string ipAddress, CancellationToken cancellationToken = default)
    {
        var tenant = new Tenant(Guid.NewGuid(), name, slug);
        await _auditService.LogSecurityEventAsync(tenant.Id, ownerUserId, "Tenant.Create", $"Creación de tenant {name} ({slug})", ipAddress, true, cancellationToken);
        return tenant;
    }
}
