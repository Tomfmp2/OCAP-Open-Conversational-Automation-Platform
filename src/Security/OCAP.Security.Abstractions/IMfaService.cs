using OCAP.Security.Abstractions.DTOs;

namespace OCAP.Security.Abstractions;

// Contrato de servicio para orquestación de Autenticación de Múltiples Factores y Códigos de Recuperación (CAP-17).
public interface IMfaService
{
    Task<MfaSetupDto> SetupMfaAsync(Guid tenantId, Guid userId, string userEmail, CancellationToken cancellationToken = default);
    Task<EnableMfaResponseDto> EnableMfaAsync(Guid tenantId, Guid userId, string code, CancellationToken cancellationToken = default);
    Task<bool> DisableMfaAsync(Guid tenantId, Guid userId, string code, CancellationToken cancellationToken = default);
    Task<bool> VerifyMfaCodeAsync(Guid tenantId, Guid userId, string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> RegenerateRecoveryCodesAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default);
}
