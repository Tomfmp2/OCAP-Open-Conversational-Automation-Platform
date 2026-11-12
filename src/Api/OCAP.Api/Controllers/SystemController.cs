using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OCAP.Api.Configuration;
using OCAP.Api.Security;
using OCAP.Infrastructure.Services;
using OCAP.Security.Abstractions;
using OCAP.Security.Abstractions.Options;

namespace OCAP.Api.Controllers;

/// <summary>Lo que guarda el panel. Un número null vuelve al valor del servidor.</summary>
public sealed record SystemSettingsRequest(
    bool? RateLimitingEnabled,
    int? RequestsPerMinute,
    int? AuthAttemptsPerMinute,
    int? WidgetMessagesPerMinute,
    int? WidgetReadsPerMinute,
    int? SessionMinutes,
    IReadOnlyList<string>? AllowedOrigins);

/// <summary>Valores del servidor: los que rigen cuando el panel deja un campo vacío.</summary>
public sealed record SystemDefaultsDto(
    bool RateLimitingEnabled,
    int RequestsPerMinute,
    int AuthAttemptsPerMinute,
    int WidgetMessagesPerMinute,
    int WidgetReadsPerMinute,
    int SessionMinutes,
    IReadOnlyList<string> AllowedOrigins);

public sealed record SystemSettingsDto(SystemSettingsSnapshot Settings, SystemDefaultsDto Defaults);

/// <summary>Ajustes de toda la instalación. Solo para la cuenta administradora del sistema.</summary>
[ApiController]
[Route("api/system/settings")]
[Authorize(Policy = OcapPermissions.SystemManage)]
public sealed class SystemController : ControllerBase
{
    private const int MaxOrigins = 20;

    private readonly SystemSettingsStore _store;
    private readonly RateLimitingSettings _rateLimiting;
    private readonly CorsSettings _cors;
    private readonly JwtOptions _jwt;
    private readonly ITenantContext _tenantContext;
    private readonly IUserContext _userContext;
    private readonly ISecurityAuditService _audit;

    public SystemController(
        SystemSettingsStore store,
        IOptions<RateLimitingSettings> rateLimiting,
        IOptions<CorsSettings> cors,
        IOptions<JwtOptions> jwt,
        ITenantContext tenantContext,
        IUserContext userContext,
        ISecurityAuditService audit)
    {
        _store = store;
        _rateLimiting = rateLimiting.Value;
        _cors = cors.Value;
        _jwt = jwt.Value;
        _tenantContext = tenantContext;
        _userContext = userContext;
        _audit = audit;
    }

    [HttpGet]
    public async Task<ActionResult<SystemSettingsDto>> Get(CancellationToken cancellationToken)
    {
        // La copia en memoria puede ir un minuto por detrás de otra instancia; quien edita ve lo guardado.
        await _store.RefreshAsync(cancellationToken);
        return Ok(ToDto(_store.Current));
    }

    [HttpPut]
    public async Task<ActionResult<SystemSettingsDto>> Update([FromBody] SystemSettingsRequest request, CancellationToken cancellationToken)
    {
        if (request is null) return Invalid("Faltan los ajustes.");

        var error = OutOfRange(request.RequestsPerMinute, 60, 10_000, "Las peticiones por minuto")
            ?? OutOfRange(request.AuthAttemptsPerMinute, 3, 100, "Los intentos de inicio de sesión por minuto")
            ?? OutOfRange(request.WidgetMessagesPerMinute, 1, 600, "Los mensajes de WebChat por minuto")
            ?? OutOfRange(request.WidgetReadsPerMinute, 10, 6_000, "Las consultas de WebChat por minuto")
            ?? OutOfRange(request.SessionMinutes, 5, 1_440, "Los minutos de sesión");
        if (error is not null) return Invalid(error);

        var origins = new List<string>();
        foreach (var raw in request.AllowedOrigins ?? [])
        {
            if (string.IsNullOrWhiteSpace(raw)) continue;
            if (!TryNormalizeOrigin(raw, out var origin))
                return Invalid($"«{raw.Trim()}» no es un dominio válido. Usa https://dominio, sin ruta.");
            if (!origins.Contains(origin, StringComparer.OrdinalIgnoreCase)) origins.Add(origin);
        }
        if (origins.Count > MaxOrigins) return Invalid($"Se admiten hasta {MaxOrigins} dominios.");

        var settings = new SystemSettingsSnapshot(
            request.RateLimitingEnabled,
            request.RequestsPerMinute,
            request.AuthAttemptsPerMinute,
            request.WidgetMessagesPerMinute,
            request.WidgetReadsPerMinute,
            request.SessionMinutes,
            origins);
        await _store.SaveAsync(settings, cancellationToken);

        await _audit.LogSecurityEventAsync(
            _tenantContext.TenantId,
            _userContext.UserId,
            "System_SettingsUpdated",
            "Ajustes del sistema actualizados.",
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
            true,
            cancellationToken);

        return Ok(ToDto(settings));
    }

    private SystemSettingsDto ToDto(SystemSettingsSnapshot settings) => new(
        settings,
        new SystemDefaultsDto(
            _rateLimiting.EnableRateLimiting,
            _rateLimiting.PermitLimit,
            _rateLimiting.AuthPermitLimit,
            _rateLimiting.WidgetMessagesPermitLimit,
            _rateLimiting.WidgetReadPermitLimit,
            _jwt.AccessTokenExpiryMinutes,
            _cors.AllowedOrigins));

    private ObjectResult Invalid(string detail) =>
        Problem(statusCode: StatusCodes.Status400BadRequest, title: "Ajustes no válidos", detail: detail);

    private static string? OutOfRange(int? value, int min, int max, string label) =>
        value is null || (value >= min && value <= max) ? null : $"{label} deben estar entre {min} y {max}.";

    // Un origen es esquema, host y puerto. Se exige https salvo en la propia máquina, igual que la dirección del panel.
    internal static bool TryNormalizeOrigin(string raw, out string origin)
    {
        origin = string.Empty;
        if (!Uri.TryCreate(raw.Trim(), UriKind.Absolute, out var uri)) return false;
        if (uri.AbsolutePath != "/" || uri.Query.Length > 0 || uri.Fragment.Length > 0 || uri.UserInfo.Length > 0) return false;
        var secure = uri.Scheme == Uri.UriSchemeHttps;
        if (!secure && !(uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback)) return false;

        origin = uri.GetLeftPart(UriPartial.Authority).ToLowerInvariant();
        return true;
    }
}
