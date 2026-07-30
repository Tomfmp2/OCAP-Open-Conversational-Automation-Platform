using OCAP.Security.Domain.Entities;

namespace OCAP.Security.Abstractions;

/// <summary>
/// Consulta de identidad para autenticación local (email/password).
/// </summary>
public interface IUserAuthenticationQuery
{
    Task<UserAuthenticationRecord?> FindByEmailAsync(string email, CancellationToken cancellationToken = default);
}

public sealed record UserAuthenticationRecord(
    UserIdentity User,
    Tenant Tenant,
    Role Role);
