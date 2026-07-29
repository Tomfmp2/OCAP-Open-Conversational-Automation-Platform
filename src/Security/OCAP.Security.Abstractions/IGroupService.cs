using OCAP.Security.Abstractions.DTOs;

namespace OCAP.Security.Abstractions;

// Contrato de servicio para la gestión de Grupos de Usuarios y sus membesías/roles (CAP-16).
public interface IGroupService
{
    Task<IReadOnlyList<GroupDto>> GetGroupsAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<GroupDto?> GetGroupByIdAsync(Guid tenantId, Guid groupId, CancellationToken cancellationToken = default);
    Task<GroupDto> CreateGroupAsync(Guid tenantId, CreateGroupRequestDto request, CancellationToken cancellationToken = default);
    Task<bool> AddUserToGroupAsync(Guid tenantId, Guid groupId, Guid userId, CancellationToken cancellationToken = default);
    Task<bool> RemoveUserFromGroupAsync(Guid tenantId, Guid groupId, Guid userId, CancellationToken cancellationToken = default);
    Task<bool> AssignRoleToGroupAsync(Guid tenantId, Guid groupId, Guid roleId, CancellationToken cancellationToken = default);
    Task<bool> DeleteGroupAsync(Guid tenantId, Guid groupId, CancellationToken cancellationToken = default);
}
