using OCAP.Security.Abstractions.DTOs;

namespace OCAP.Security.Abstractions;

// Contrato para implementación completa de SCIM 2.0 (RFC 7643 / RFC 7644) (CAP-19).
public interface IScimService
{
    Task<ScimListResponseDto<ScimUserDto>> GetUsersAsync(Guid tenantId, int startIndex = 1, int count = 100, string? filter = null, CancellationToken cancellationToken = default);
    Task<ScimUserDto?> GetUserByIdAsync(Guid tenantId, string id, CancellationToken cancellationToken = default);
    Task<ScimUserDto> CreateUserAsync(Guid tenantId, ScimUserDto dto, CancellationToken cancellationToken = default);
    Task<ScimUserDto?> UpdateUserAsync(Guid tenantId, string id, ScimUserDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteUserAsync(Guid tenantId, string id, CancellationToken cancellationToken = default);

    Task<ScimListResponseDto<ScimGroupDto>> GetGroupsAsync(Guid tenantId, int startIndex = 1, int count = 100, string? filter = null, CancellationToken cancellationToken = default);
    Task<ScimGroupDto?> GetGroupByIdAsync(Guid tenantId, string id, CancellationToken cancellationToken = default);
    Task<ScimGroupDto> CreateGroupAsync(Guid tenantId, ScimGroupDto dto, CancellationToken cancellationToken = default);
    Task<ScimGroupDto?> UpdateGroupAsync(Guid tenantId, string id, ScimGroupDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteGroupAsync(Guid tenantId, string id, CancellationToken cancellationToken = default);

    Task<object> ProcessBulkRequestAsync(Guid tenantId, ScimBulkRequestDto request, CancellationToken cancellationToken = default);
    object GetServiceProviderConfig();
    object GetResourceTypes();
    object GetSchemas();
}
