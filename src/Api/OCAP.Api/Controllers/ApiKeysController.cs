using Microsoft.AspNetCore.Mvc;
using OCAP.Api.Models.Security;
using OCAP.Security.Application.UseCases;

namespace OCAP.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ApiKeysController : ControllerBase
{
    private readonly CreateApiKeyUseCase _createApiKeyUseCase;

    public ApiKeysController(CreateApiKeyUseCase createApiKeyUseCase)
    {
        _createApiKeyUseCase = createApiKeyUseCase ?? throw new ArgumentNullException(nameof(createApiKeyUseCase));
    }

    [HttpGet]
    public ActionResult<List<ApiKeyDto>> GetApiKeys()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var keys = new List<ApiKeyDto>
        {
            new(Guid.NewGuid(), tenantId, userId, "ocap_live_", "Integración WhatsApp", DateTime.UtcNow.AddYears(1), false, DateTime.UtcNow.AddHours(-2)),
            new(Guid.NewGuid(), tenantId, userId, "ocap_live_", "Servicio Zapier", DateTime.UtcNow.AddMonths(6), false, DateTime.UtcNow.AddDays(-1))
        };
        return Ok(keys);
    }

    [HttpPost]
    public async Task<ActionResult<CreateApiKeyResponseDto>> CreateApiKey([FromBody] string name, CancellationToken cancellationToken)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
        var result = await _createApiKeyUseCase.ExecuteAsync(Guid.NewGuid(), Guid.NewGuid(), name ?? "Nueva API Key", TimeSpan.FromDays(365), ip, cancellationToken);
        return Ok(new CreateApiKeyResponseDto(result.RawApiKey, result.ApiKeyId, result.Prefix, result.Name, result.ExpiresAtUtc));
    }
}
