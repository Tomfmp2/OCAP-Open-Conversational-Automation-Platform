using Microsoft.AspNetCore.Mvc;
using OCAP.Api.Models.Security;
using OCAP.Security.Application.UseCases;

namespace OCAP.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TenantsController : ControllerBase
{
    private readonly CreateTenantUseCase _createTenantUseCase;

    public TenantsController(CreateTenantUseCase createTenantUseCase)
    {
        _createTenantUseCase = createTenantUseCase ?? throw new ArgumentNullException(nameof(createTenantUseCase));
    }

    [HttpGet]
    public ActionResult<List<TenantDto>> GetTenants()
    {
        var tenants = new List<TenantDto>
        {
            new(Guid.NewGuid(), "Organización Principal", "org-principal", true, DateTime.UtcNow.AddDays(-60)),
            new(Guid.NewGuid(), "Empresa Demo SaaS", "demo-saas", true, DateTime.UtcNow.AddDays(-15))
        };
        return Ok(tenants);
    }

    [HttpPost]
    public async Task<ActionResult<TenantDto>> CreateTenant([FromBody] CreateTenantRequestDto request, CancellationToken cancellationToken)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
        var tenant = await _createTenantUseCase.ExecuteAsync(request.Name, request.Slug, Guid.NewGuid(), ip, cancellationToken);
        return Ok(new TenantDto(tenant.Id, tenant.Name, tenant.Slug, tenant.IsActive, tenant.CreatedAtUtc));
    }
}
