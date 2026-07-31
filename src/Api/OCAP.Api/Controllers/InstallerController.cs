using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OCAP.Api.Installation;

namespace OCAP.Api.Controllers;

[ApiController]
[Route("api/installer")]
public sealed class InstallerController : ControllerBase
{
    private readonly IInstallationSetupService _setupService;
    private readonly InstallationArtifactStore _store;
    private readonly IConfiguration _configuration;
    private readonly IValidator<InstallerSetupRequest> _validator;

    public InstallerController(
        IInstallationSetupService setupService,
        InstallationArtifactStore store,
        IConfiguration configuration,
        IValidator<InstallerSetupRequest> validator)
    {
        _setupService = setupService;
        _store = store;
        _configuration = configuration;
        _validator = validator;
    }

    [HttpGet("status")]
    [AllowAnonymous]
    public async Task<ActionResult<InstallerStatusResponse>> GetStatus(CancellationToken cancellationToken)
    {
        var status = await _setupService.GetStatusAsync(cancellationToken);
        return Ok(status);
    }

    [HttpPost("setup")]
    [AllowAnonymous]
    public async Task<ActionResult<InstallerSetupResponse>> Setup(
        [FromBody] InstallerSetupRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new
            {
                message = "Validación fallida.",
                errors = validation.Errors.Select(e => new { e.PropertyName, e.ErrorMessage })
            });
        }

        var completed = _store.IsCompleted(_configuration);
        var authenticated = User.Identity?.IsAuthenticated ?? false;
        var isLocal = string.Equals(request.Target, "Local", StringComparison.OrdinalIgnoreCase);
        // Local self-hosted: permitir reaplicar admin/producto sin login (evita quedarse fuera).
        // Web: tras la primera instalación exige autenticación.
        if (completed && !authenticated && !isLocal)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                message = "La instalación ya está completa. Autentícate como admin para reconfigurar."
            });
        }

        var result = await _setupService.ApplyAsync(request, cancellationToken);
        return Ok(result);
    }
}
