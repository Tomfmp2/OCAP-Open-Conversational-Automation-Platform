using OCAP.Security.Domain.Entities;

namespace OCAP.Security.Abstractions;

// Contrato para la gestión, rotación y revocación persistente de Refresh Tokens.
public interface IRefreshTokenService
{
    // Genera y almacena un nuevo Refresh Token para un usuario.
    Task<RefreshToken> CreateRefreshTokenAsync(Guid userId, TimeSpan? expiry = null, CancellationToken cancellationToken = default);

    // Valida y rota un Refresh Token existente generando uno nuevo.
    Task<RefreshToken?> ValidateAndRotateRefreshTokenAsync(string token, TimeSpan? newExpiry = null, CancellationToken cancellationToken = default);

    // Revoca un Refresh Token específico.
    Task<bool> RevokeRefreshTokenAsync(string token, string? replacedByToken = null, CancellationToken cancellationToken = default);

    // Revoca todos los Refresh Tokens activos de un usuario.
    Task<int> RevokeUserRefreshTokensAsync(Guid userId, CancellationToken cancellationToken = default);
}
