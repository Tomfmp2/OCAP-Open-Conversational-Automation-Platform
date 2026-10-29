using OCAP.Api.Security;
using Microsoft.AspNetCore.Authorization;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OCAP.Api.Models.Dashboard;
using OCAP.Api.Services;
using OCAP.Core.Entities;
using OCAP.Infrastructure.Persistence.Context;
using OCAP.Providers.Google.Abstractions;
using OCAP.Security.Abstractions;

namespace OCAP.Api.Controllers;

public record ConnectIntegrationRequestDto(
    string AuthCode,
    string? RedirectUri,
    string? Scopes,
    string? State = null
);

public record IntegrationStatusDto(
    string Provider,
    bool IsConnected,
    string AccountEmail,
    string OAuthStatus,
    List<string> GrantedScopes,
    DateTime LastSyncedAt
);

[ApiController]
[Route("api/integrations")]
[Authorize(Policy = OcapPermissions.OAuthManage)]
public class IntegrationsController : ControllerBase
{
    private readonly OCAPDbContext _dbContext;
    private readonly IUserContext _userContext;
    private readonly ITenantContext _tenantContext;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly GoogleOAuthClient _google;
    private readonly GoogleOAuthConnectionStore _googleConnections;
    private readonly ILogger<IntegrationsController> _logger;

    public IntegrationsController(
        OCAPDbContext dbContext,
        IUserContext userContext,
        ITenantContext tenantContext,
        IHttpClientFactory httpClientFactory,
        GoogleOAuthClient google,
        GoogleOAuthConnectionStore googleConnections,
        ILogger<IntegrationsController> logger)
    {
        _logger = logger;
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _google = google ?? throw new ArgumentNullException(nameof(google));
        _googleConnections = googleConnections ?? throw new ArgumentNullException(nameof(googleConnections));
    }

    // GET /api/integrations - Retorna el estado de todas las integraciones soportadas
    [HttpGet]
    public async Task<ActionResult<IEnumerable<IntegrationStatusDto>>> GetAllIntegrations(CancellationToken cancellationToken)
    {
        var connections = await _dbContext.OAuthConnections.ToListAsync(cancellationToken);
        var providers = new[] { "Google", "Microsoft", "Slack", "GitHub" };

        var result = providers.Select(p =>
        {
            var conn = connections.FirstOrDefault(c => string.Equals(c.Provider, p, StringComparison.OrdinalIgnoreCase));
            return new IntegrationStatusDto(
                Provider: p,
                IsConnected: conn != null,
                AccountEmail: conn != null ? string.Empty : string.Empty,
                OAuthStatus: conn != null ? (conn.TokenExpiration > DateTime.UtcNow ? "Active" : "Expired") : "Not Authorized",
                GrantedScopes: conn != null && !string.IsNullOrEmpty(conn.Scopes) ? conn.Scopes.Split(',').ToList() : new List<string>(),
                LastSyncedAt: conn?.UpdatedAt ?? DateTime.MinValue
            );
        }).ToList();

        return Ok(result);
    }

    [HttpGet("google")]
    public async Task<ActionResult<GoogleIntegrationDto>> GetGoogleIntegration(CancellationToken cancellationToken)
    {
        var conn = await _dbContext.OAuthConnections.FirstOrDefaultAsync(c => c.Provider == "Google", cancellationToken);
        var integration = new GoogleIntegrationDto
        {
            IsConnected = conn != null,
            AccountEmail = string.Empty,
            OAuthStatus = conn != null ? (conn.TokenExpiration > DateTime.UtcNow ? "Authorized" : "Expired") : "Not Authorized",
            GrantedScopes = conn != null && !string.IsNullOrEmpty(conn.Scopes) ? conn.Scopes.Split(',').ToList() : new List<string>(),
            LastSyncedAt = conn?.UpdatedAt ?? DateTime.MinValue
        };

        return Ok(integration);
    }

    /// <summary>
    /// Dirección de consentimiento de Google con el cliente OAuth de la organización. El panel redirige ahí y Google
    /// vuelve a <paramref name="redirectUri"/> (registrada en Google Cloud) con el código y el state firmado.
    /// </summary>
    [HttpGet("google/authorize-url")]
    public async Task<IActionResult> GetGoogleAuthorizeUrl(
        [FromQuery] string redirectUri,
        [FromServices] GoogleOAuthState state,
        CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(redirectUri, UriKind.Absolute, out var redirect) || (redirect.Scheme != Uri.UriSchemeHttps && redirect.Host != "localhost"))
            return BadRequest(new { message = "La dirección de retorno debe ser https (o localhost)." });
        if (await _google.ResolveAsync(_tenantContext.TenantId, cancellationToken) is not { } settings)
            return BadRequest(new { message = "Configura el cliente OAuth de Google en Ajustes." });

        var scopes = settings.Scopes.Count > 0 ? settings.Scopes : GoogleOAuthClient.DefaultScopes.ToList();
        var query = new Dictionary<string, string>
        {
            ["response_type"] = "code",
            ["client_id"] = settings.ClientId,
            ["redirect_uri"] = redirectUri,
            ["scope"] = string.Join(' ', scopes),
            // Refresh token para que el asistente siga usando Gmail/Calendar sin volver a pedir permiso.
            ["access_type"] = "offline",
            ["prompt"] = "consent",
            ["state"] = state.Create(_tenantContext.TenantId, _userContext.UserId)
        };
        var url = "https://accounts.google.com/o/oauth2/v2/auth?" + string.Join('&', query.Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"));
        return Ok(new { url });
    }

    [HttpGet("{provider}")]
    public async Task<ActionResult<IntegrationStatusDto>> GetIntegrationStatus(string provider, CancellationToken cancellationToken)
    {
        var conn = await _dbContext.OAuthConnections
            .FirstOrDefaultAsync(c => c.Provider.ToLower() == provider.ToLower(), cancellationToken);

        var dto = new IntegrationStatusDto(
            Provider: provider,
            IsConnected: conn != null,
            AccountEmail: string.Empty,
            OAuthStatus: conn != null ? (conn.TokenExpiration > DateTime.UtcNow ? "Authorized" : "Expired") : "Not Authorized",
            GrantedScopes: conn != null && !string.IsNullOrEmpty(conn.Scopes) ? conn.Scopes.Split(',').ToList() : new List<string>(),
            LastSyncedAt: conn?.UpdatedAt ?? DateTime.MinValue
        );

        return Ok(dto);
    }

    [HttpPost("{provider}/connect")]
    public async Task<ActionResult<IntegrationStatusDto>> ConnectIntegration(
        string provider,
        [FromBody] ConnectIntegrationRequestDto request,
        [FromServices] GoogleOAuthState state,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.AuthCode))
        {
            return BadRequest("El código de autorización es requerido.");
        }

        if (!_userContext.IsAuthenticated || _userContext.UserId == Guid.Empty)
        {
            return Unauthorized();
        }

        if (!string.Equals(provider, "Google", StringComparison.OrdinalIgnoreCase))
        {
            return StatusCode(StatusCodes.Status501NotImplemented,
                $"El intercambio OAuth real para '{provider}' aún no está implementado.");
        }

        // Sin un state emitido por esta API para esta misma sesión, el código podría venir de otra cuenta (CSRF).
        if (!state.IsValid(request.State, _tenantContext.TenantId, _userContext.UserId))
        {
            return BadRequest("La conexión con Google caducó o no se inició desde este panel. Vuelve a intentarlo.");
        }

        if (await _google.ResolveAsync(_tenantContext.TenantId, cancellationToken) is not { } googleSettings)
        {
            return BadRequest("Configura el cliente OAuth de Google en Ajustes.");
        }

        var redirectUri = string.IsNullOrWhiteSpace(request.RedirectUri)
            ? _google.ServerSettings.RedirectUri
            : request.RedirectUri;
        if (string.IsNullOrWhiteSpace(redirectUri))
        {
            return BadRequest("RedirectUri es requerido (en la petición o en Google:RedirectUri).");
        }

        GoogleTokenGrant grant;
        try
        {
            grant = await _google.ExchangeCodeAsync(_tenantContext.TenantId, request.AuthCode, redirectUri, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Google rechazó el intercambio del código OAuth.");
            return BadRequest("No se pudo intercambiar el código OAuth de Google. Vuelve a conectar la cuenta.");
        }

        var scopes = !string.IsNullOrWhiteSpace(grant.Scope)
            ? grant.Scope.Replace(' ', ',')
            : (request.Scopes ?? string.Join(',', googleSettings.Scopes));
        var accountEmail = await TryGetGoogleAccountEmailAsync(grant.AccessToken, cancellationToken)
            ?? _userContext.Email
            ?? "connected@google.com";

        await _googleConnections.SaveAsync(_tenantContext.TenantId, _userContext.UserId, grant, scopes, cancellationToken);

        return Ok(new IntegrationStatusDto(
            Provider: provider,
            IsConnected: true,
            AccountEmail: accountEmail,
            OAuthStatus: "Authorized",
            GrantedScopes: scopes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList(),
            LastSyncedAt: DateTime.UtcNow
        ));
    }

    [HttpPost("{provider}/disconnect")]
    public async Task<IActionResult> DisconnectIntegration(string provider, CancellationToken cancellationToken)
    {
        var existing = await _dbContext.OAuthConnections
            .FirstOrDefaultAsync(c => c.Provider.ToLower() == provider.ToLower(), cancellationToken);

        if (existing != null)
        {
            _dbContext.OAuthConnections.Remove(existing);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return Ok(new { Message = $"Integración con {provider} desconectada exitosamente." });
    }

    [HttpPost("{provider}/sync")]
    public async Task<IActionResult> SyncIntegration(string provider, CancellationToken cancellationToken)
    {
        var existing = await _dbContext.OAuthConnections
            .FirstOrDefaultAsync(c => c.Provider.ToLower() == provider.ToLower(), cancellationToken);

        if (existing == null)
        {
            return BadRequest($"No existe una conexión activa para el proveedor {provider}.");
        }

        // Sincronizar = comprobar que Google sigue aceptando la conexión renovando el access token.
        if (await _googleConnections.GetAccessTokenAsync(_tenantContext.TenantId, cancellationToken, forceRefresh: true) is null)
        {
            return BadRequest($"Google ya no acepta la conexión de {provider}. Vuelve a conectar la cuenta.");
        }

        return Ok(new { Message = $"Sincronización completada con {provider}.", SyncedAt = DateTime.UtcNow });
    }

    private async Task<string?> TryGetGoogleAccountEmailAsync(string accessToken, CancellationToken cancellationToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            using var request = new HttpRequestMessage(HttpMethod.Get, "https://www.googleapis.com/oauth2/v3/userinfo");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            using var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            return document.RootElement.TryGetProperty("email", out var email)
                ? email.GetString()
                : null;
        }
        catch
        {
            return null;
        }
    }
}
