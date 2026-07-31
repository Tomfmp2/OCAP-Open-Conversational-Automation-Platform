using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OCAP.Api.Installation;
using OCAP.Api.Models.Dashboard;
using OCAP.Core.Entities;
using OCAP.Infrastructure.Persistence.Context;
using OCAP.Providers.Google.Abstractions;
using OCAP.Security.Abstractions;

namespace OCAP.Api.Controllers;

public record ConnectIntegrationRequestDto(
    string AuthCode,
    string? RedirectUri,
    string? Scopes
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
public class IntegrationsController : ControllerBase
{
    private readonly OCAPDbContext _dbContext;
    private readonly IUserContext _userContext;
    private readonly ITenantContext _tenantContext;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly InstallationArtifactStore _artifactStore;

    public IntegrationsController(
        OCAPDbContext dbContext,
        IUserContext userContext,
        ITenantContext tenantContext,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        InstallationArtifactStore artifactStore)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _artifactStore = artifactStore ?? throw new ArgumentNullException(nameof(artifactStore));
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
    public async Task<ActionResult<IntegrationStatusDto>> ConnectIntegration(string provider, [FromBody] ConnectIntegrationRequestDto request, CancellationToken cancellationToken)
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

        var googleSettings = _configuration.GetSection(GoogleWorkspaceOptions.SectionName).Get<GoogleSettings>()
            ?? new GoogleSettings();
        if (string.IsNullOrWhiteSpace(googleSettings.ClientId) ||
            string.IsNullOrWhiteSpace(googleSettings.ClientSecret))
        {
            return StatusCode(StatusCodes.Status501NotImplemented,
                "Se requieren Google:ClientId y Google:ClientSecret para intercambiar el código OAuth.");
        }

        var redirectUri = string.IsNullOrWhiteSpace(request.RedirectUri)
            ? googleSettings.RedirectUri
            : request.RedirectUri;
        if (string.IsNullOrWhiteSpace(redirectUri))
        {
            return BadRequest("RedirectUri es requerido (en la petición o en Google:RedirectUri).");
        }

        GoogleTokenResponse tokenResponse;
        try
        {
            tokenResponse = await ExchangeGoogleAuthCodeAsync(
                request.AuthCode,
                googleSettings.ClientId,
                googleSettings.ClientSecret,
                redirectUri,
                cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            return BadRequest($"No se pudo intercambiar el código OAuth de Google: {ex.Message}");
        }

        if (string.IsNullOrWhiteSpace(tokenResponse.AccessToken))
        {
            return BadRequest("Google no devolvió un access_token válido.");
        }

        var scopes = !string.IsNullOrWhiteSpace(tokenResponse.Scope)
            ? tokenResponse.Scope.Replace(' ', ',')
            : (request.Scopes ?? string.Join(',', googleSettings.Scopes));
        var expiration = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn > 0 ? tokenResponse.ExpiresIn : 3600);
        var accountEmail = await TryGetGoogleAccountEmailAsync(tokenResponse.AccessToken, cancellationToken)
            ?? _userContext.Email
            ?? "connected@google.com";

        var existing = await _dbContext.OAuthConnections
            .FirstOrDefaultAsync(c => c.Provider.ToLower() == provider.ToLower(), cancellationToken);

        if (existing != null)
        {
            existing.UpdateTokens(tokenResponse.AccessToken, tokenResponse.RefreshToken ?? string.Empty, expiration);
        }
        else
        {
            var newConn = new OAuthConnection(
                Guid.NewGuid(),
                _userContext.UserId,
                provider,
                tokenResponse.AccessToken,
                tokenResponse.RefreshToken ?? string.Empty,
                expiration,
                scopes,
                _tenantContext.TenantId
            );
            _dbContext.OAuthConnections.Add(newConn);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _artifactStore.MergeGoogleAccessTokenAsync(tokenResponse.AccessToken, cancellationToken);

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

        existing.UpdateTokens(existing.AccessToken, existing.RefreshToken, existing.TokenExpiration);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new { Message = $"Sincronización completada con {provider}.", SyncedAt = existing.UpdatedAt });
    }

    private async Task<GoogleTokenResponse> ExchangeGoogleAuthCodeAsync(
        string authCode,
        string clientId,
        string clientSecret,
        string redirectUri,
        CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient();
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["code"] = authCode,
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret,
            ["redirect_uri"] = redirectUri,
            ["grant_type"] = "authorization_code"
        });

        using var response = await client.PostAsync("https://oauth2.googleapis.com/token", content, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Google token endpoint respondió {(int)response.StatusCode}: {body}",
                null,
                response.StatusCode);
        }

        var parsed = JsonSerializer.Deserialize<GoogleTokenResponse>(body, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return parsed ?? throw new HttpRequestException("Respuesta de token de Google vacía o inválida.");
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

    private sealed class GoogleTokenResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("scope")]
        public string? Scope { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("token_type")]
        public string? TokenType { get; set; }
    }
}
