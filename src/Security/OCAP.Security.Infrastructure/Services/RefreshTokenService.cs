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

    public async Task<RefreshToken> CreateRefreshTokenAsync(Guid userId, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty) throw new ArgumentException("El UserId no puede ser vacío.", nameof(userId));

        var randomBytes = RandomNumberGenerator.GetBytes(64);
        var tokenString = Convert.ToBase64String(randomBytes);
        var validFor = expiry ?? TimeSpan.FromDays(7);

        var refreshToken = new RefreshToken(
            Guid.NewGuid(),
            userId,
            tokenString,
            DateTime.UtcNow.Add(validFor)
        );

        _dbContext.RefreshTokens.Add(refreshToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger?.LogInformation("Refresh Token generado para usuario {UserId} (ID: {Id})", userId, refreshToken.Id);
        return refreshToken;
    }

    public async Task<RefreshToken?> ValidateAndRotateRefreshTokenAsync(string token, TimeSpan? newExpiry = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;

        var existingToken = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == token, cancellationToken);

        if (existingToken == null || !existingToken.IsActive)
        {
            _logger?.LogWarning("Intento de rotación fallido: Refresh Token no encontrado o revocado/expirado.");
            return null;
        }

        // Crear nuevo Refresh Token para rotación
        var newToken = await CreateRefreshTokenAsync(existingToken.UserId, newExpiry, cancellationToken);

        // Revocar el token anterior marcando el reemplazo
        existingToken.Revoke(newToken.Token);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger?.LogInformation("Refresh Token {OldId} rotado exitosamente a {NewId} para usuario {UserId}",
            existingToken.Id, newToken.Id, existingToken.UserId);

        return newToken;
    }

    public async Task<bool> RevokeRefreshTokenAsync(string token, string? replacedByToken = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token)) return false;

        var existingToken = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == token, cancellationToken);

        if (existingToken == null || existingToken.IsRevoked) return false;

        existingToken.Revoke(replacedByToken);
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
            token.Revoke("USER_ALL_TOKENS_REVOKED");
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger?.LogInformation("Se revocaron {Count} Refresh Tokens para el usuario {UserId}", activeTokens.Count, userId);
        return activeTokens.Count;
    }
}
