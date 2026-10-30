using OCAP.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OCAP.Infrastructure.Persistence.Context;
using OCAP.Security.Abstractions;

namespace OCAP.Api.Controllers;

/// <summary>Datos de la organización que ve y edita el panel.</summary>
public sealed record OrganizationSettingsDto(string Name);

/// <summary>SMTP de la organización. <c>Password</c> null conserva la guardada; vacía la borra.</summary>
public sealed record UpdateGoogleSettingsRequest(string ClientId, string? ClientSecret);

public sealed record UpdateEmailSettingsRequest(string Host, int Port, string? Username, string? Password, string From, bool UseSsl);

[ApiController]
[Route("api/[controller]")]
public class SettingsController : ControllerBase
{
    private const int MaxNameLength = 256;

    private readonly OCAPDbContext _dbContext;
    private readonly ITenantContext _tenantContext;
    private readonly IUserContext _userContext;
    private readonly ISecurityAuditService _auditService;

    public SettingsController(
        OCAPDbContext dbContext,
        ITenantContext tenantContext,
        IUserContext userContext,
        ISecurityAuditService auditService)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
    }

    [HttpGet]
    public async Task<ActionResult<OrganizationSettingsDto>> GetSettings(CancellationToken cancellationToken)
    {
        var tenant = await FindTenantAsync(cancellationToken);
        return tenant is null ? NotFound() : Ok(new OrganizationSettingsDto(tenant.Name));
    }

    [HttpPut]
    [Authorize(Policy = OcapPermissions.SettingsManage)]
    public async Task<ActionResult<OrganizationSettingsDto>> UpdateSettings([FromBody] OrganizationSettingsDto request, CancellationToken cancellationToken)
    {
        var name = request?.Name?.Trim() ?? string.Empty;
        if (name.Length == 0 || name.Length > MaxNameLength)
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: $"El nombre debe tener entre 1 y {MaxNameLength} caracteres.");

        var tenant = await FindTenantAsync(cancellationToken);
        if (tenant is null) return NotFound();

        var previous = tenant.Name;
        tenant.Rename(name);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditService.LogSecurityEventAsync(
            tenant.Id,
            _userContext.UserId,
            "Organization_Renamed",
            $"Organización renombrada de '{previous}' a '{tenant.Name}'.",
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
            true,
            cancellationToken);

        return Ok(new OrganizationSettingsDto(tenant.Name));
    }

    // Solo la organización de la sesión: sin ella no hay datos (antes devolvía la primera de la base).
    private Task<OCAP.Security.Domain.Entities.Tenant?> FindTenantAsync(CancellationToken cancellationToken) =>
        _tenantContext.TenantId == Guid.Empty
            ? Task.FromResult<OCAP.Security.Domain.Entities.Tenant?>(null)
            : _dbContext.Tenants.FirstOrDefaultAsync(t => t.Id == _tenantContext.TenantId, cancellationToken);

    // El correo saliente lleva los enlaces de «Olvidé mi contraseña»: quien lo controla podría leerlos. Por eso lo
    // gestiona el mismo permiso que el equipo (Security.Manage), no el de ajustes generales.

    /// <summary>SMTP propio de la organización, sin la contraseña. <c>usesServer</c>: sin uno propio, se usa el del servidor.</summary>
    [HttpGet("email")]
    [Authorize(Policy = OcapPermissions.SecurityManage)]
    public async Task<IActionResult> GetEmailSettings(
        [FromServices] OCAP.Api.Team.OrganizationEmailSettings email,
        [FromServices] Microsoft.Extensions.Options.IOptions<OCAP.Api.Team.SmtpOptions> server,
        CancellationToken cancellationToken)
    {
        var own = await email.DescribeAsync(_tenantContext.TenantId, cancellationToken);
        return Ok(new { settings = own, usesServer = own is null && server.Value.IsComplete });
    }

    [HttpPut("email")]
    [Authorize(Policy = OcapPermissions.SecurityManage)]
    public async Task<IActionResult> UpdateEmailSettings(
        [FromBody] UpdateEmailSettingsRequest request,
        [FromServices] OCAP.Api.Team.OrganizationEmailSettings email,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Host) || string.IsNullOrWhiteSpace(request.From))
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Indica el servidor y la dirección de envío.");
        if (!OCAP.Api.Team.OrganizationEmailSettings.AllowedPorts.Contains(request.Port))
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Usa un puerto de correo: 25, 465, 587 o 2525.");
        if (!System.Net.Mail.MailAddress.TryCreate(request.From.Trim(), out _))
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "La dirección de envío no es un correo válido.");
        if (await OCAP.Api.Team.OrganizationEmailSettings.ValidateHostAsync(request.Host.Trim(), cancellationToken) is { } blocked)
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: blocked);

        await email.SaveAsync(
            _tenantContext.TenantId,
            new OCAP.Api.Team.OrganizationEmailSettingsDto(request.Host, request.Port, request.Username, request.From, request.UseSsl, false),
            request.Password,
            cancellationToken);
        await AuditAsync("Organization_EmailSettingsUpdated", $"SMTP de la organización: {request.Host.Trim()}:{request.Port}.", cancellationToken);
        return NoContent();
    }

    [HttpDelete("email")]
    [Authorize(Policy = OcapPermissions.SecurityManage)]
    public async Task<IActionResult> DeleteEmailSettings([FromServices] OCAP.Api.Team.OrganizationEmailSettings email, CancellationToken cancellationToken)
    {
        await email.ClearAsync(_tenantContext.TenantId, cancellationToken);
        await AuditAsync("Organization_EmailSettingsRemoved", "SMTP de la organización eliminado.", cancellationToken);
        return NoContent();
    }

    /// <summary>Envía un correo de prueba a quien lo pide, con el SMTP que usaría la organización.</summary>
    [HttpPost("email/test")]
    [Authorize(Policy = OcapPermissions.SecurityManage)]
    public async Task<IActionResult> TestEmailSettings([FromServices] OCAP.Api.Team.IEmailSender sender, CancellationToken cancellationToken)
    {
        var to = await _dbContext.UserIdentities.Where(u => u.Id == _userContext.UserId).Select(u => u.Email).FirstOrDefaultAsync(cancellationToken);
        if (to is null) return NotFound();
        if (!await sender.IsConfiguredAsync(_tenantContext.TenantId, cancellationToken))
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Configura el servidor de correo primero.");
        try
        {
            await sender.SendAsync(_tenantContext.TenantId, to, "Prueba de correo de OCAP", "El correo de tu organización en OCAP funciona.", cancellationToken);
        }
        catch (Exception ex) when (ex is System.Net.Mail.SmtpException or InvalidOperationException or System.Net.Sockets.SocketException or IOException)
        {
            // Solo una categoría: el texto crudo del servidor serviría para sondear qué hay detrás de un host o puerto.
            HttpContext.RequestServices.GetRequiredService<ILogger<SettingsController>>()
                .LogWarning(ex, "Falló el correo de prueba de la organización {TenantId}.", _tenantContext.TenantId);
            var reason = ex is System.Net.Mail.SmtpException { StatusCode: System.Net.Mail.SmtpStatusCode.MustIssueStartTlsFirst or System.Net.Mail.SmtpStatusCode.ClientNotPermitted }
                || (ex is System.Net.Mail.SmtpException smtp && smtp.Message.Contains("authentic", StringComparison.OrdinalIgnoreCase))
                ? "El servidor rechazó el usuario o la contraseña, o exige TLS."
                : "No se pudo conectar con el servidor de correo. Revisa el servidor, el puerto y TLS.";
            return Problem(statusCode: StatusCodes.Status422UnprocessableEntity, title: "No se pudo enviar el correo de prueba.", detail: reason);
        }
        return Ok(new { sentTo = to });
    }

    private Task AuditAsync(string action, string detail, CancellationToken cancellationToken) =>
        _auditService.LogSecurityEventAsync(
            _tenantContext.TenantId,
            _userContext.UserId,
            action,
            detail,
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
            true,
            cancellationToken);

    [HttpGet("preferences")]
    public async Task<IActionResult> GetPreferences(CancellationToken cancellationToken) =>
        Ok(await OCAP.Infrastructure.Services.TenantSettingsSections.ReadAsync<OCAP.Infrastructure.Services.OrganizationPreferences>(
               _dbContext, _tenantContext.TenantId, OCAP.Infrastructure.Services.OrganizationPreferences.Section, cancellationToken)
           ?? new OCAP.Infrastructure.Services.OrganizationPreferences(null, null, null, null));

    [HttpPut("preferences")]
    [Authorize(Policy = OcapPermissions.SettingsManage)]
    public async Task<IActionResult> UpdatePreferences(
        [FromBody] OCAP.Infrastructure.Services.OrganizationPreferences request,
        [FromServices] IAuthorizationService authorization,
        CancellationToken cancellationToken)
    {
        string? Clean(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();
        var language = Clean(request.Language);
        var timeZone = Clean(request.TimeZone);
        var panelUrl = Clean(request.PanelUrl)?.TrimEnd('/');

        if (language is not null and not ("es" or "en"))
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Idioma no admitido: es o en.");
        if (timeZone is not null && !TimeZoneInfo.TryFindSystemTimeZoneById(timeZone, out _))
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: $"Zona horaria desconocida: {timeZone}.");
        if (panelUrl is not null && (!Uri.TryCreate(panelUrl, UriKind.Absolute, out var url) || (url.Scheme != Uri.UriSchemeHttps && url.Host != "localhost")))
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "La dirección del panel debe empezar por https://.");
        if (request.ConversationRetentionDays is { } days
            && days is < OCAP.Infrastructure.Services.OrganizationPreferences.MinRetentionDays or > OCAP.Infrastructure.Services.OrganizationPreferences.MaxRetentionDays)
            return Problem(statusCode: StatusCodes.Status400BadRequest,
                title: $"La retención va de {OCAP.Infrastructure.Services.OrganizationPreferences.MinRetentionDays} a {OCAP.Infrastructure.Services.OrganizationPreferences.MaxRetentionDays} días.");

        // La dirección del panel va en los enlaces de invitación y de recuperación de contraseña: quien la cambia podría
        // desviar esos tokens a otro dominio. Solo quien administra la seguridad.
        var stored = await OCAP.Infrastructure.Services.TenantSettingsSections.ReadAsync<OCAP.Infrastructure.Services.OrganizationPreferences>(
            _dbContext, _tenantContext.TenantId, OCAP.Infrastructure.Services.OrganizationPreferences.Section, cancellationToken);
        if (panelUrl != stored?.PanelUrl
            && !(await authorization.AuthorizeAsync(User, OcapPermissions.SecurityManage)).Succeeded)
            return Problem(statusCode: StatusCodes.Status403Forbidden, title: "Cambiar la dirección del panel requiere el permiso de seguridad.");

        if (request.MemoryDays is { } memoryDays && memoryDays is < 0 or > OCAP.Infrastructure.Services.OrganizationPreferences.MaxRetentionDays)
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "La memoria va de 0 (desactivada) a 3650 días.");
        var notice = Clean(request.PrivacyNotice);
        if (notice is { Length: > OCAP.Infrastructure.Services.OrganizationPreferences.MaxPrivacyNoticeLength })
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "El aviso de privacidad admite hasta 600 caracteres.");

        var preferences = new OCAP.Infrastructure.Services.OrganizationPreferences(
            language, timeZone, panelUrl, request.ConversationRetentionDays, request.MemoryDays, notice);
        await OCAP.Infrastructure.Services.TenantSettingsSections.WriteAsync(
            _dbContext, _tenantContext.TenantId, OCAP.Infrastructure.Services.OrganizationPreferences.Section, preferences, cancellationToken);
        await AuditAsync("Organization_PreferencesUpdated",
            $"Preferencias: idioma {language ?? "-"}, zona {timeZone ?? "-"}, panel {panelUrl ?? "-"}, retención {request.ConversationRetentionDays?.ToString() ?? "sin límite"}.",
            cancellationToken);
        return Ok(preferences);
    }

    /// <summary>Cliente OAuth de Google de la organización, sin el secreto. Lo gestiona quien administra integraciones.</summary>
    [HttpGet("google")]
    [Authorize(Policy = OcapPermissions.OAuthManage)]
    public async Task<IActionResult> GetGoogleSettings(
        [FromServices] OCAP.Api.Services.OrganizationGoogleSettings google,
        [FromServices] OCAP.Api.Services.GoogleOAuthClient client,
        CancellationToken cancellationToken)
    {
        var clientId = await google.GetClientIdAsync(_tenantContext.TenantId, cancellationToken);
        var server = client.ServerSettings;
        return Ok(new
        {
            clientId,
            usesServer = clientId is null && !string.IsNullOrWhiteSpace(server.ClientId) && !string.IsNullOrWhiteSpace(server.ClientSecret)
        });
    }

    [HttpPut("google")]
    [Authorize(Policy = OcapPermissions.OAuthManage)]
    public async Task<IActionResult> UpdateGoogleSettings(
        [FromBody] UpdateGoogleSettingsRequest request,
        [FromServices] OCAP.Api.Services.OrganizationGoogleSettings google,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ClientId) || !request.ClientId.Trim().EndsWith(".apps.googleusercontent.com", StringComparison.Ordinal))
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "El client ID de Google termina en .apps.googleusercontent.com.");
        try
        {
            await google.SaveAsync(_tenantContext.TenantId, request.ClientId, string.IsNullOrWhiteSpace(request.ClientSecret) ? null : request.ClientSecret.Trim(), cancellationToken);
        }
        catch (ArgumentException ex)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: ex.Message);
        }
        await AuditAsync("Organization_GoogleOAuthUpdated", "Cliente OAuth de Google actualizado.", cancellationToken);
        return NoContent();
    }

    [HttpDelete("google")]
    [Authorize(Policy = OcapPermissions.OAuthManage)]
    public async Task<IActionResult> DeleteGoogleSettings([FromServices] OCAP.Api.Services.OrganizationGoogleSettings google, CancellationToken cancellationToken)
    {
        await google.ClearAsync(_tenantContext.TenantId, cancellationToken);
        await AuditAsync("Organization_GoogleOAuthRemoved", "Cliente OAuth de Google eliminado.", cancellationToken);
        return NoContent();
    }
}
