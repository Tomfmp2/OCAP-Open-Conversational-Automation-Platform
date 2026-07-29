using OCAP.Security.Abstractions.DTOs;

namespace OCAP.Security.Abstractions;

// Servicio orquestador de autenticación y vinculación con proveedores externos (CAP-15).
public interface IExternalAuthenticationService
{
    // Devuelve la lista de proveedores externos habilitados.
    Task<IReadOnlyList<ExternalProviderInfoDto>> GetEnabledProvidersAsync(CancellationToken cancellationToken = default);

    // Inicia el desafío de inicio de sesión con un proveedor externo específico.
    Task<ExternalAuthChallengeDto> InitiateChallengeAsync(string provider, string redirectUri, Guid? tenantId = null, CancellationToken cancellationToken = default);

    // Procesa el callback del proveedor externo, aprovisiona/vincula el usuario y emite tokens OCAP.
    Task<ExternalAuthLoginResultDto> ProcessCallbackAsync(ExternalAuthCallbackRequestDto request, Guid tenantId, CancellationToken cancellationToken = default);

    // Vincula un proveedor externo al usuario autenticado actual.
    Task<bool> LinkProviderAsync(Guid tenantId, Guid userId, string provider, string code, string redirectUri, CancellationToken cancellationToken = default);

    // Desvincula un proveedor externo del usuario actual.
    Task<bool> UnlinkProviderAsync(Guid tenantId, Guid userId, string provider, CancellationToken cancellationToken = default);

    // Obtiene la lista de proveedores vinculados al usuario actual.
    Task<IReadOnlyList<string>> GetLinkedProvidersAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default);
}
