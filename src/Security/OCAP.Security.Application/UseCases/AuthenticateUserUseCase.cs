using OCAP.Security.Abstractions;
using OCAP.Security.Domain.Entities;

namespace OCAP.Security.Application.UseCases;

public record AuthenticationResult(
    string AccessToken,
    string RefreshToken,
    Guid UserId,
    Guid TenantId,
    string Email,
    string RoleName
);

public class AuthenticateUserUseCase
{
    // Hash de una contraseña aleatoria, calculado una vez, contra el que se compara cuando el correo no existe.
    private static (string Hash, string Salt)? _timingDecoy;

    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ISecurityAuditService _auditService;
    private readonly IUserAuthenticationQuery _userQuery;
    private readonly IRefreshTokenService _refreshTokenService;

    public AuthenticateUserUseCase(
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        ISecurityAuditService auditService,
        IUserAuthenticationQuery userQuery,
        IRefreshTokenService refreshTokenService)
    {
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _jwtTokenService = jwtTokenService ?? throw new ArgumentNullException(nameof(jwtTokenService));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _userQuery = userQuery ?? throw new ArgumentNullException(nameof(userQuery));
        _refreshTokenService = refreshTokenService ?? throw new ArgumentNullException(nameof(refreshTokenService));
    }

    public async Task<AuthenticationResult?> ExecuteAsync(string email, string password, string ipAddress, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password)) return null;

        var record = await _userQuery.FindByEmailAsync(email, cancellationToken);
        if (record is null)
        {
            // Mismo costo que una contraseña real: el tiempo de respuesta no revela qué correos existen.
            var decoy = _timingDecoy ??= _passwordHasher.HashPassword(Guid.NewGuid().ToString("N"));
            _passwordHasher.VerifyPassword(password, decoy.Hash, decoy.Salt);
            await _auditService.LogSecurityEventAsync(Guid.Empty, Guid.Empty, "User.Login", $"Login fallido (usuario inexistente): {email}", ipAddress, false, cancellationToken);
            return null;
        }

        if (record.User.IsLockedOut(DateTime.UtcNow))
        {
            await _auditService.LogSecurityEventAsync(record.Tenant.Id, record.User.Id, "User.Login", $"Login bloqueado: {email}", ipAddress, false, cancellationToken);
            return null;
        }

        var isValid = _passwordHasher.VerifyPassword(password, record.User.PasswordHash, record.User.Salt);
        await _auditService.LogSecurityEventAsync(
            record.Tenant.Id,
            record.User.Id,
            "User.Login",
            $"Intento de login para {email}",
            ipAddress,
            isValid,
            cancellationToken);
        await _userQuery.RecordLoginAttemptAsync(record.User.Id, isValid, cancellationToken);

        if (!isValid) return null;

        var accessToken = _jwtTokenService.GenerateAccessToken(record.User, record.Tenant, record.Role, record.Role.Permissions);
        var refreshToken = await _refreshTokenService.CreateRefreshTokenAsync(record.User.Id, cancellationToken: cancellationToken);

        return new AuthenticationResult(
            accessToken,
            refreshToken.Token,
            record.User.Id,
            record.Tenant.Id,
            record.User.Email,
            record.Role.Name);
    }
}
