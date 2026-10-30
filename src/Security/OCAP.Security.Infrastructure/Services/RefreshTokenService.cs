using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OCAP.Infrastructure.Persistence.Context;
using OCAP.Security.Abstractions;
using OCAP.Security.Domain.Entities;

namespace OCAP.Security.Infrastructure.Services;

// Servicio persistente de Refresh Tokens respaldado por EF Core con soporte de rotación criptográfica.
public class RefreshTokenService : IRefreshTokenService
{
    private readonly OCAPDbContext _dbContext;
    private readonly ILogger<RefreshTokenService>? _logger;

    public RefreshTokenService(OCAPDbContext dbContext, ILogger<RefreshTokenService>? logger = null)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger;
    }

    public async Task<IssuedRefreshToken> CreateRefreshTokenAsync(Guid userId, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty) throw new ArgumentException("El UserId no puede ser vacío.", nameof(userId));

        var user = await _dbContext.UserIdentities
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive, cancellationToken)
            ?? throw new InvalidOperationException($"Usuario {userId} no encontrado para emitir refresh token.");

        if (user.TenantId == Guid.Empty)
        {
            throw new InvalidOperationException("El usuario no tiene TenantId asociado.");
        }

        return await IssueAsync(userId, user.TenantId, Guid.Empty, expiry, cancellationToken);
    }

    public async Task<IssuedRefreshToken?> ValidateAndRotateRefreshTokenAsync(string token, TimeSpan? newExpiry = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;

        // El token es el secreto que aísla la sesión: la búsqueda no depende del tenant de la petición.
        var hash = RefreshToken.Hash(token);
        var existing = await _dbContext.RefreshTokens
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);

        if (existing is null) return null;

        if (existing.WasRotated)
        {
            // Un token ya rotado vuelve a aparecer: lo tiene alguien más. Se cierra la sesión en todos lados.
            var revoked = await RevokeFamilyAsync(existing.FamilyId, cancellationToken);
            _logger?.LogWarning(
                "Reutilización del refresh token {TokenId} del usuario {UserId}: se revocaron {Count} tokens de la sesión.",
                existing.Id, existing.UserId, revoked);
            return null;
        }

        if (!existing.IsActive) return null;

        var newValue = NewTokenValue();
        // Revocar el anterior e insertar el sucesor es una sola transacción: nadie ve el token como rotado sin que
        // su sucesor exista, así revocar la familia siempre alcanza al token nuevo.
        await using var transaction = _dbContext.Database.IsRelational()
            ? await _dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;

        if (!await TryRevokeAsync(existing.Id, RefreshToken.Hash(newValue), cancellationToken))
        {
            // Otra petición rotó este mismo token a la vez. El panel serializa las renovaciones entre pestañas, así
            // que dos usos simultáneos indican una copia: se trata como reutilización.
            if (transaction is not null) await transaction.RollbackAsync(cancellationToken);
            await RevokeFamilyAsync(existing.FamilyId, cancellationToken);
            _logger?.LogWarning("Uso simultáneo del refresh token {TokenId}: se revocó la sesión.", existing.Id);
            return null;
        }

        var issued = await IssueAsync(existing.UserId, existing.TenantId, existing.FamilyId, newExpiry, cancellationToken, newValue);
        if (transaction is not null) await transaction.CommitAsync(cancellationToken);
        _logger?.LogInformation("Refresh Token {OldId} rotado para el usuario {UserId}.", existing.Id, existing.UserId);
        return issued;
    }

    public async Task<bool> RevokeRefreshTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token)) return false;

        var hash = RefreshToken.Hash(token);
        var existingToken = await _dbContext.RefreshTokens
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);

        if (existingToken == null || existingToken.IsRevoked) return false;

        existingToken.Revoke();
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger?.LogInformation("Refresh Token {Id} fue revocado.", existingToken.Id);
        return true;
    }

    public async Task<int> RevokeUserRefreshTokensAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var activeTokens = await _dbContext.RefreshTokens
            .Where(t => t.UserId == userId && !t.IsRevoked)
            .ToListAsync(cancellationToken);

        if (activeTokens.Count == 0) return 0;

        foreach (var token in activeTokens)
        {
            token.Revoke();
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger?.LogInformation("Se revocaron {Count} Refresh Tokens para el usuario {UserId}", activeTokens.Count, userId);
        return activeTokens.Count;
    }

    private async Task<IssuedRefreshToken> IssueAsync(
        Guid userId, Guid tenantId, Guid familyId, TimeSpan? expiry, CancellationToken cancellationToken, string? value = null)
    {
        value ??= NewTokenValue();
        var refreshToken = new RefreshToken(
            Guid.NewGuid(), userId, RefreshToken.Hash(value), DateTime.UtcNow.Add(expiry ?? TimeSpan.FromDays(7)), tenantId, familyId);

        _dbContext.RefreshTokens.Add(refreshToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return new IssuedRefreshToken(value, userId, refreshToken.ExpiresAtUtc);
    }

    // Marca el token como rotado solo si seguía vigente: en Postgres es un UPDATE condicional, así dos peticiones
    // con el mismo token no obtienen ambas uno nuevo.
    private async Task<bool> TryRevokeAsync(Guid id, string replacedByHash, CancellationToken cancellationToken)
    {
        if (_dbContext.Database.IsRelational())
        {
            var updated = await _dbContext.RefreshTokens
                .IgnoreQueryFilters()
                .Where(t => t.Id == id && !t.IsRevoked)
                .ExecuteUpdateAsync(u => u
                    .SetProperty(t => t.IsRevoked, true)
                    .SetProperty(t => t.ReplacedByTokenHash, replacedByHash), cancellationToken);
            return updated == 1;
        }

        var tracked = await _dbContext.RefreshTokens.IgnoreQueryFilters().FirstAsync(t => t.Id == id, cancellationToken);
        if (tracked.IsRevoked) return false;
        tracked.Revoke(replacedByHash);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<int> RevokeFamilyAsync(Guid familyId, CancellationToken cancellationToken)
    {
        var active = await _dbContext.RefreshTokens
            .IgnoreQueryFilters()
            .Where(t => t.FamilyId == familyId && !t.IsRevoked)
            .ToListAsync(cancellationToken);
        foreach (var token in active) token.Revoke();
        await _dbContext.SaveChangesAsync(cancellationToken);
        return active.Count;
    }

    private static string NewTokenValue() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
}
