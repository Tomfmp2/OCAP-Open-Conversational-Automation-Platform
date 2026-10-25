using System.Security.Claims;
using OCAP.Security.Domain.Entities;

namespace OCAP.Security.Abstractions;

// Contrato para la generación, validación y rotación de tokens JWT seguros.
public interface IJwtTokenService
{
    // Genera un Access Token JWT con claims de usuario, tenant, rol y permisos.
    string GenerateAccessToken(UserIdentity user, Tenant tenant, Role role, IEnumerable<string> permissions);

    // Genera un Refresh Token criptográficamente seguro.
    RefreshToken GenerateRefreshToken(Guid userId);

    // Valida la firma y vigencia de un Access Token.
    ClaimsPrincipal? ValidateToken(string token);

    // Desafío de segundo factor tras una contraseña correcta: otra audiencia y 5 minutos, no sirve como Access Token.
    string GenerateMfaChallengeToken(Guid userId, Guid tenantId);

    // Usuario y tenant del desafío si la firma, la audiencia y la vigencia son válidas.
    (Guid UserId, Guid TenantId)? ValidateMfaChallengeToken(string token);
}
