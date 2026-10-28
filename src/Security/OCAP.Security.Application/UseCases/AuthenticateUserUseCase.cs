using OCAP.Security.Abstractions;
using OCAP.Security.Domain.Entities;

namespace OCAP.Security.Application.UseCases;

public record AuthenticationResult(
    string AccessToken,
    IssuedRefreshToken? RefreshToken,
    Guid UserId,
    Guid TenantId,
    string Email,
    string RoleName,
    // Contraseña correcta pero falta el segundo factor: no hay tokens de sesión, solo el desafío.
    bool MfaRequired = false,
    string? MfaToken = null
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
    private readonly IMfaService _mfaService;

    public AuthenticateUserUseCase(
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        ISecurityAuditService auditService,
        IUserAuthenticationQuery userQuery,
        IRefreshTokenService refreshTokenService,
        IMfaService mfaService)
    {
        _mfaService = mfaService ?? throw new ArgumentNullException(nameof(mfaService));
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

        if (_passwordHasher.NeedsRehash(record.User.PasswordHash))
        {
            var (hash, salt) = _passwordHasher.HashPassword(password);
            await _userQuery.UpgradePasswordHashAsync(record.User.Id, hash, salt, cancellationToken);
        }

        if (await _userQuery.IsMfaEnabledAsync(record.User.Id, cancellationToken))
        {
            return new AuthenticationResult(
                string.Empty,
                null,
                record.User.Id,
                record.Tenant.Id,
                record.User.Email,
                record.Role.Name,
                MfaRequired: true,
                MfaToken: _jwtTokenService.GenerateMfaChallengeToken(record.User.Id, record.Tenant.Id));
        }

        return await IssueSessionAsync(record, cancellationToken);
    }

    /// <summary>
    /// Segundo paso del login con MFA: el desafío emitido tras la contraseña más un código TOTP o de recuperación.
    /// Los códigos incorrectos cuentan para el bloqueo de la cuenta.
    /// </summary>
    public async Task<AuthenticationResult?> CompleteMfaAsync(string mfaToken, string code, string ipAddress, CancellationToken cancellationToken = default)
    {
        if (_jwtTokenService.ValidateMfaChallengeToken(mfaToken) is not { } challenge) return null;

        var record = await _userQuery.FindByIdAsync(challenge.UserId, cancellationToken);
        if (record is null || record.Tenant.Id != challenge.TenantId || record.User.IsLockedOut(DateTime.UtcNow)) return null;

        var verified = await _mfaService.VerifyMfaCodeAsync(record.Tenant.Id, record.User.Id, code, cancellationToken);
        await _auditService.LogSecurityEventAsync(
            record.Tenant.Id, record.User.Id, "User.LoginMfa", $"Segundo factor para {record.User.Email}", ipAddress, verified, cancellationToken);
        await _userQuery.RecordLoginAttemptAsync(record.User.Id, verified, cancellationToken);

        return verified ? await IssueSessionAsync(record, cancellationToken) : null;
    }

    private async Task<AuthenticationResult> IssueSessionAsync(UserAuthenticationRecord record, CancellationToken cancellationToken)
    {
        var accessToken = _jwtTokenService.GenerateAccessToken(record.User, record.Tenant, record.Role, record.Role.Permissions);
        var refreshToken = await _refreshTokenService.CreateRefreshTokenAsync(record.User.Id, cancellationToken: cancellationToken);

        return new AuthenticationResult(
            accessToken,
            refreshToken,
            record.User.Id,
            record.Tenant.Id,
            record.User.Email,
            record.Role.Name);
    }
}
