namespace OCAP.Security.Abstractions.DTOs;

// DTOs estandarizados para SCIM 2.0 (RFC 7643/7644) y sincronización de directorios LDAP/Active Directory (CAP-19).

public record LdapConfigDto(
    Guid Id,
    Guid TenantId,
    string Server,
    int Port,
    bool UseSsl,
    string BindDn,
    string BaseDn,
    string UserSearchFilter,
    string GroupSearchFilter,
    bool IsEnabled
);

public record SaveLdapConfigDto(
    string Server,
    int Port,
    bool UseSsl,
    string BindDn,
    string? BindPassword,
    string BaseDn,
    string? UserSearchFilter = null,
    string? GroupSearchFilter = null
);

public record SyncStatusDto(
    Guid JobId,
    Guid TenantId,
    string ProviderType,
    string Status,
    DateTime? LastSyncAtUtc,
    int TotalUsersSynced,
    int TotalGroupsSynced,
    string? LastErrorMessage
);

public record SyncHistoryDto(
    Guid Id,
    Guid JobId,
    string SyncType,
    string Status,
    int UsersCreated,
    int UsersUpdated,
    int UsersDeprovisioned,
    int GroupsSynced,
    string? ErrorLog,
    DateTime ExecutedAtUtc
);

public record ScimUserDto(
    string id,
    string? externalId,
    string userName,
    ScimNameDto? name,
    List<ScimEmailDto> emails,
    bool active,
    List<string> schemas
);

public record ScimNameDto(
    string? formatted,
    string? familyName,
    string? givenName
);

public record ScimEmailDto(
    string value,
    string type,
    bool primary
);

public record ScimGroupDto(
    string id,
    string? externalId,
    string displayName,
    List<ScimGroupMemberDto> members,
    List<string> schemas
);

public record ScimGroupMemberDto(
    string value,
    string? display,
    string? refUrl
);

public record ScimListResponseDto<T>(
    int totalResults,
    int startIndex,
    int itemsPerPage,
    List<string> schemas,
    List<T> Resources
);

public record ScimErrorDto(
    string status,
    string scimType,
    string detail,
    List<string> schemas
);

public record ScimBulkRequestDto(
    List<ScimBulkOperationDto> Operations,
    List<string> schemas
);

public record ScimBulkOperationDto(
    string method,
    string path,
    string? bulkId,
    object? data
);
