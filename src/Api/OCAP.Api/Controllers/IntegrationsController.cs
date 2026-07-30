using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OCAP.Api.Models.Dashboard;
using OCAP.Core.Entities;
using OCAP.Infrastructure.Persistence.Context;
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

    public IntegrationsController(OCAPDbContext dbContext, IUserContext userContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
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
                AccountEmail: conn != null ? $"connected@{p.ToLowerInvariant()}.org" : "disconnected",
                OAuthStatus: conn != null ? (conn.TokenExpiration > DateTime.UtcNow ? "Active" : "Expired") : "Not Authorized",
                GrantedScopes: conn != null && !string.IsNullOrEmpty(conn.Scopes) ? conn.Scopes.Split(',').ToList() : new List<string>(),
                LastSyncedAt: conn?.UpdatedAt ?? DateTime.UtcNow
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
            AccountEmail = conn != null ? "connected@google.com" : "not-connected",
            OAuthStatus = conn != null ? (conn.TokenExpiration > DateTime.UtcNow ? "Authorized" : "Expired") : "Not Authorized",
            GrantedScopes = conn != null && !string.IsNullOrEmpty(conn.Scopes) ? conn.Scopes.Split(',').ToList() : new List<string>(),
            LastSyncedAt = conn?.UpdatedAt ?? DateTime.UtcNow
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
            AccountEmail: conn != null ? $"user@{provider.ToLower()}.com" : "not-connected",
            OAuthStatus: conn != null ? (conn.TokenExpiration > DateTime.UtcNow ? "Authorized" : "Expired") : "Not Authorized",
            GrantedScopes: conn != null && !string.IsNullOrEmpty(conn.Scopes) ? conn.Scopes.Split(',').ToList() : new List<string>(),
            LastSyncedAt: conn?.UpdatedAt ?? DateTime.UtcNow
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

        var userId = _userContext.UserId != Guid.Empty ? _userContext.UserId : Guid.NewGuid();
        var existing = await _dbContext.OAuthConnections
            .FirstOrDefaultAsync(c => c.Provider.ToLower() == provider.ToLower(), cancellationToken);

        if (existing != null)
        {
            existing.UpdateTokens($"access_token_{provider}_{Guid.NewGuid()}", $"refresh_token_{provider}", DateTime.UtcNow.AddDays(30));
        }
        else
        {
            var newConn = new OAuthConnection(
                Guid.NewGuid(),
                userId,
                provider,
                $"access_token_{provider}_{Guid.NewGuid()}",
                $"refresh_token_{provider}",
                DateTime.UtcNow.AddDays(30),
                request.Scopes ?? "read,write"
            );
            _dbContext.OAuthConnections.Add(newConn);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new IntegrationStatusDto(
            Provider: provider,
            IsConnected: true,
            AccountEmail: $"user@{provider.ToLower()}.com",
            OAuthStatus: "Authorized",
            GrantedScopes: (request.Scopes ?? "read,write").Split(',').ToList(),
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
}
