using OCAP.Security.Abstractions.DTOs;

namespace OCAP.Security.Abstractions;

// Contrato de servicio para la administración integral de usuarios (CAP-16).
public interface IUserManagementService
{
    Task<IReadOnlyList<UserDetailDto>> GetUsersAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<UserDetailDto?> GetUserByIdAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default);
    Task<UserDetailDto> InviteUserAsync(Guid tenantId, InviteUserRequestDto request, CancellationToken cancellationToken = default);
    Task<bool> LockUserAsync(Guid tenantId, Guid userId, TimeSpan? duration = null, CancellationToken cancellationToken = default);
    Task<bool> UnlockUserAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default);
    Task<bool> ActivateUserAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default);
    Task<bool> DeactivateUserAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default);
    Task<bool> ResetPasswordAsync(Guid tenantId, ResetPasswordRequestDto request, CancellationToken cancellationToken = default);
    Task<bool> ChangePasswordAsync(Guid tenantId, Guid userId, ChangePasswordRequestDto request, CancellationToken cancellationToken = default);
    Task<bool> VerifyEmailAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default);
}
