namespace OCAP.Security.Abstractions;

// Token recién emitido: el valor en claro solo existe aquí, para entregarlo al cliente; la base guarda su hash.
public sealed record IssuedRefreshToken(string Token, Guid UserId, DateTime ExpiresAtUtc);

// Contrato para la gestión, rotación y revocación persistente de Refresh Tokens.
public interface IRefreshTokenService
{
    // Genera y almacena un nuevo Refresh Token para un usuario (inicia una sesión nueva).
    Task<IssuedRefreshToken> CreateRefreshTokenAsync(Guid userId, TimeSpan? expiry = null, CancellationToken cancellationToken = default);

    // Valida y rota un Refresh Token. Si el token ya se había rotado, revoca toda la sesión y devuelve null.
    Task<IssuedRefreshToken?> ValidateAndRotateRefreshTokenAsync(string token, TimeSpan? newExpiry = null, CancellationToken cancellationToken = default);

    // Revoca un Refresh Token específico.
    Task<bool> RevokeRefreshTokenAsync(string token, CancellationToken cancellationToken = default);

    // Revoca todos los Refresh Tokens activos de un usuario.
    Task<int> RevokeUserRefreshTokensAsync(Guid userId, CancellationToken cancellationToken = default);
}
