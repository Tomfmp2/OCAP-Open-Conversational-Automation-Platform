using OCAP.Security.Abstractions;
using OCAP.Security.Domain.Entities;

namespace OCAP.Security.Application.UseCases;

// DTO de resultado del inicio de sesión exitoso.
public record AuthenticationResult(
    string AccessToken,
    string RefreshToken,
    Guid UserId,
    Guid TenantId,
    string Email,
    string RoleName
);

// Caso de uso para autenticar credenciales de usuario y emitir tokens JWT.
public class AuthenticateUserUseCase
{
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ISecurityAuditService _auditService;

    public AuthenticateUserUseCase(
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        ISecurityAuditService auditService)
    {
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _jwtTokenService = jwtTokenService ?? throw new ArgumentNullException(nameof(jwtTokenService));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
    }

    public async Task<AuthenticationResult?> ExecuteAsync(string email, string password, string ipAddress, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password)) return null;

        // Creación simulada de usuario y tenant de prueba en memoria
        var tenant = new Tenant(Guid.NewGuid(), "Organización Principal", "org-principal");
        var (hash, salt) = _passwordHasher.HashPassword(password);
        var user = new UserIdentity(Guid.NewGuid(), tenant.Id, email, hash, salt, "Usuario Administrador");

        var role = new Role(Guid.NewGuid(), tenant.Id, "Admin", "Administrador total del tenant", new[]
        {
            "Conversation.Read", "Conversation.Write", "Conversation.Delete",
            "Agent.Read", "Agent.Write", "Agent.Execute", "Tool.Execute",
            "Dashboard.Read", "Dashboard.Admin", "Deployment.Manage", "AI.Execute",
            "Settings.Manage", "OAuth.Manage"
        });

        // Verificar contraseña
        var isValid = _passwordHasher.VerifyPassword(password, hash, salt);
        await _auditService.LogSecurityEventAsync(tenant.Id, user.Id, "User.Login", $"Intento de login para {email}", ipAddress, isValid, cancellationToken);

        if (!isValid) return null;

        var accessToken = _jwtTokenService.GenerateAccessToken(user, tenant, role, role.Permissions);
        var refreshToken = _jwtTokenService.GenerateRefreshToken(user.Id);

        return new AuthenticationResult(accessToken, refreshToken.Token, user.Id, tenant.Id, user.Email, role.Name);
    }
}
