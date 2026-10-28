using OCAP.Api.Security;
using Microsoft.AspNetCore.RateLimiting;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OCAP.Api.Models.Security;
using OCAP.Api.Setup;
using OCAP.Security.Application.UseCases;
using OCAP.Security.Infrastructure.Services;

namespace OCAP.Api.Controllers;

/// <summary>
/// Configuración inicial del producto: crea la organización y el primer administrador, y deja la sesión iniciada.
/// Solo existe mientras la base no tiene usuarios; después todo se gestiona desde el panel (IA, canales, ajustes).
/// La infraestructura (puertos, base de datos, secretos) no se configura aquí: la genera scripts/ocap-setup.sh.
/// </summary>
[ApiController]
[Route("api/setup")]
[AllowAnonymous]
public sealed class SetupController : ControllerBase
{
    private readonly SetupCodeService _setupCode;
    private readonly InitialAdminProvisioner _provisioner;
    private readonly AuthenticateUserUseCase _authenticate;
    private readonly IValidator<SetupRequest> _validator;
    private readonly ILogger<SetupController> _logger;

    public SetupController(
        SetupCodeService setupCode,
        InitialAdminProvisioner provisioner,
        AuthenticateUserUseCase authenticate,
        IValidator<SetupRequest> validator,
        ILogger<SetupController> logger)
    {
        _setupCode = setupCode;
        _provisioner = provisioner;
        _authenticate = authenticate;
        _validator = validator;
        _logger = logger;
    }

    [HttpGet("status")]
    [ProducesResponseType(typeof(SetupStatusResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<SetupStatusResponse>> GetStatus(CancellationToken cancellationToken)
    {
        var required = await _setupCode.IsPendingAsync(cancellationToken);
        // Si el admin se creó por otra vía (bootstrap, importación), el código emitido al arrancar ya no sirve.
        if (required) _setupCode.EnsureCode();
        else _setupCode.Invalidate();
        return Ok(new SetupStatusResponse(required));
    }

    [EnableRateLimiting(OCAP.Api.Configuration.RateLimitingSettings.AuthPolicy)]
    [HttpPost]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<LoginResponseDto>> Complete([FromBody] SetupRequest request, CancellationToken cancellationToken)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            foreach (var error in validation.Errors)
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            return ValidationProblem(ModelState);
        }

        await _setupCode.CompletionLock.WaitAsync(cancellationToken);
        try
        {
            // Dentro del candado: un segundo envío simultáneo ve el admin recién creado.
            if (!await _setupCode.IsPendingAsync(cancellationToken))
            {
                _setupCode.Invalidate();
                return Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "La configuración inicial ya se completó",
                    detail: "Inicia sesión con la cuenta de administrador.");
            }

            if (!_setupCode.Verify(request.SetupCode))
            {
                _logger.LogWarning("Intento de configuración inicial con un código inválido desde {Ip}.",
                    HttpContext.Connection.RemoteIpAddress);
                return Problem(
                    statusCode: StatusCodes.Status403Forbidden,
                    title: "Código de configuración inválido",
                    detail: "Copia el código que imprime la API en sus logs al arrancar (docker compose logs ocap-api).");
            }

            await _provisioner.CreateAsync(
                new InitialAdminRequest(
                    request.AdminEmail,
                    request.AdminPassword,
                    request.AdminName,
                    request.OrganizationName,
                    SetupRequestValidator.ToSlug(request.OrganizationName)),
                cancellationToken);
            _setupCode.Invalidate();
        }
        finally
        {
            _setupCode.CompletionLock.Release();
        }

        _logger.LogInformation("Configuración inicial completada. Administrador: {Email}.", request.AdminEmail);

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
        var session = await _authenticate.ExecuteAsync(request.AdminEmail, request.AdminPassword, ip, cancellationToken);
        if (session is null)
        {
            // La cuenta existe; solo falló el inicio de sesión automático.
            return Problem(statusCode: StatusCodes.Status500InternalServerError,
                title: "Administrador creado, pero no se pudo iniciar sesión",
                detail: "Inicia sesión manualmente con el correo y la contraseña que configuraste.");
        }

        RefreshTokenCookie.Append(Response, session.RefreshToken!);
        return Ok(new LoginResponseDto(session.AccessToken, session.UserId, session.TenantId, session.Email, session.RoleName));
    }
}
