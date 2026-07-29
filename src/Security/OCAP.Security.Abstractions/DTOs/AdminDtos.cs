namespace OCAP.Security.Abstractions.DTOs;

// DTOs para la administración de usuarios, grupos, roles y organizaciones (CAP-16).

public record UserDetailDto(
    Guid Id,
    Guid TenantId,
    string Email,
    string FullName,
    bool IsActive,
    bool IsLocked,
    bool IsEmailVerified,
    DateTime CreatedAtUtc
);

public record InviteUserRequestDto(
    string Email,
    string FullName,
    string? RoleName = null
);

public record ResetPasswordRequestDto(
    string Email
);

public record ChangePasswordRequestDto(
    string CurrentPassword,
    string NewPassword
);

public record UpdateProfileRequestDto(
    string FullName
);

public record GroupDto(
    Guid Id,
    Guid TenantId,
    string Name,
    string Description,
    DateTime CreatedAtUtc,
    int UserCount = 0
);

public record CreateGroupRequestDto(
    string Name,
    string? Description = null
);

public record TenantDetailDto(
    Guid Id,
    string Name,
    string Slug,
    DateTime CreatedAtUtc,
    int MemberCount = 0
);

public record AddTenantMemberRequestDto(
    Guid UserId,
    string Role
);
