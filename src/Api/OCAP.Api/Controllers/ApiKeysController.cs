using Microsoft.AspNetCore.Mvc;
using OCAP.Api.Models.Security;
using OCAP.Api.Security;
using OCAP.Security.Abstractions;
using OCAP.Security.Application.UseCases;

namespace OCAP.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ApiKeysController : ControllerBase
{
    private readonly IApiKeyService _apiKeyService;
    private readonly CreateApiKeyUseCase _createApiKeyUseCase;
    private readonly RevokeApiKeyUseCase _revokeApiKeyUseCase;
    private readonly ITenantContext _tenantContext;
    private readonly IUserContext _userContext;

    public ApiKeysController(
        IApiKeyService apiKeyService,
        CreateApiKeyUseCase createApiKeyUseCase,
        RevokeApiKeyUseCase revokeApiKeyUseCase,
        ITenantContext tenantContext,
        IUserContext userContext)
    {
        _apiKeyService = apiKeyService ?? throw new ArgumentNullException(nameof(apiKeyService));
        _createApiKeyUseCase = createApiKeyUseCase ?? throw new ArgumentNullException(nameof(createApiKeyUseCase));
        _revokeApiKeyUseCase = revokeApiKeyUseCase ?? throw new ArgumentNullException(nameof(revokeApiKeyUseCase));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
    }

    [HttpGet]
    public async Task<ActionResult<List<ApiKeyDto>>> GetApiKeys(CancellationToken cancellationToken)
    {
        var tenantId = TenantSecurity.RequireTenantId(_tenantContext);
        var entities = await _apiKeyService.GetApiKeysForTenantAsync(tenantId, cancellationToken);

        var dtos = entities.Select(e => new ApiKeyDto(
            e.Id, e.TenantId, e.UserId, e.Prefix, e.Name, e.ExpiresAtUtc, e.IsRevoked, e.CreatedAtUtc
        )).ToList();

        return Ok(dtos);
    }

    [HttpPost]
    public async Task<ActionResult<CreateApiKeyResponseDto>> CreateApiKey([FromBody] CreateApiKeyRequestDto request, CancellationToken cancellationToken)
    {
        var tenantId = TenantSecurity.RequireTenantId(_tenantContext);
        var userId = TenantSecurity.RequireUserId(_userContext);
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        var result = await _createApiKeyUseCase.ExecuteAsync(
            tenantId, userId, request?.Name ?? "Nueva API Key", TimeSpan.FromDays(365), ip, cancellationToken);
        return Ok(new CreateApiKeyResponseDto(result.RawApiKey, result.ApiKeyId, result.Prefix, result.Name, result.ExpiresAtUtc));
    }

    [HttpDelete("{id}")]
    [HttpPost("{id}/revoke")]
    public async Task<IActionResult> RevokeApiKey(Guid id, CancellationToken cancellationToken)
    {
        var tenantId = TenantSecurity.RequireTenantId(_tenantContext);
        var userId = TenantSecurity.RequireUserId(_userContext);
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        var success = await _revokeApiKeyUseCase.ExecuteAsync(id, tenantId, userId, ip, cancellationToken);
        if (!success)
        {
            return NotFound(new ProblemDetails
            {
                Title = "No encontrado",
                Detail = "Clave de API no encontrada o ya revocada.",
                Status = StatusCodes.Status404NotFound
            });
        }

        return Ok(new { message = "Clave de API revocada correctamente." });
    }
}

public class CreateApiKeyRequestDto
{
    public string Name { get; set; } = string.Empty;
}
