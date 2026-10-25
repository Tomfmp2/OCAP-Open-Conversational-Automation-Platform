using OCAP.Security.Domain.Entities;

namespace OCAP.Security.Abstractions;

/// <summary>
/// Consulta de identidad para autenticación local (email/password).
/// </summary>
public interface IUserAuthenticationQuery
{
    Task<UserAuthenticationRecord?> FindByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<UserAuthenticationRecord?> FindByIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>true si el usuario activó el segundo factor (TOTP).</summary>
    Task<bool> IsMfaEnabledAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Guarda el resultado del intento (contador de fallos y bloqueo temporal).</summary>
    Task RecordLoginAttemptAsync(Guid userId, bool succeeded, CancellationToken cancellationToken = default);
}

public sealed record UserAuthenticationRecord(
    UserIdentity User,
    Tenant Tenant,
    Role Role);
