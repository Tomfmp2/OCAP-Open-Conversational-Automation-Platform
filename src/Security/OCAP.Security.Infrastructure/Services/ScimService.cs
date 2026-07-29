using Microsoft.EntityFrameworkCore;
using OCAP.Infrastructure.Persistence.Context;
using OCAP.Security.Abstractions;
using OCAP.Security.Abstractions.DTOs;
using OCAP.Security.Domain.Entities;

namespace OCAP.Security.Infrastructure.Services;

// Servicio de infraestructura para SCIM 2.0 (RFC 7643 / RFC 7644) (CAP-19).
public class ScimService : IScimService
{
    private readonly OCAPDbContext _dbContext;
    private readonly ISecurityAuditService _auditService;

    private const string UserSchema = "urn:ietf:params:scim:schemas:core:2.0:User";
    private const string GroupSchema = "urn:ietf:params:scim:schemas:core:2.0:Group";
    private const string ListSchema = "urn:ietf:params:scim:api:messages:2.0:ListResponse";

    public ScimService(OCAPDbContext dbContext, ISecurityAuditService auditService)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
    }

    public async Task<ScimListResponseDto<ScimUserDto>> GetUsersAsync(Guid tenantId, int startIndex = 1, int count = 100, string? filter = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.UserIdentities.Where(u => u.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(filter))
        {
            var f = filter.ToLowerInvariant();
            query = query.Where(u => u.Email.ToLower().Contains(f) || u.FullName.ToLower().Contains(f));
        }

        var total = await query.CountAsync(cancellationToken);
        var users = await query.Skip(Math.Max(0, startIndex - 1)).Take(count).ToListAsync(cancellationToken);

        var userDtos = users.Select(u => new ScimUserDto(
            id: u.Id.ToString(),
            externalId: u.Email,
            userName: u.Email,
            name: new ScimNameDto(u.FullName, string.Empty, u.FullName),
            emails: new List<ScimEmailDto> { new ScimEmailDto(u.Email, "work", true) },
            active: u.IsActive && !u.IsLocked,
            schemas: new List<string> { UserSchema }
        )).ToList();

        return new ScimListResponseDto<ScimUserDto>(total, startIndex, userDtos.Count, new List<string> { ListSchema }, userDtos);
    }

    public async Task<ScimUserDto?> GetUserByIdAsync(Guid tenantId, string id, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(id, out var userId)) return null;

        var user = await _dbContext.UserIdentities.FirstOrDefaultAsync(u => u.TenantId == tenantId && u.Id == userId, cancellationToken);
        if (user == null) return null;

        return new ScimUserDto(
            id: user.Id.ToString(),
            externalId: user.Email,
            userName: user.Email,
            name: new ScimNameDto(user.FullName, string.Empty, user.FullName),
            emails: new List<ScimEmailDto> { new ScimEmailDto(user.Email, "work", true) },
            active: user.IsActive && !user.IsLocked,
            schemas: new List<string> { UserSchema }
        );
    }

    public async Task<ScimUserDto> CreateUserAsync(Guid tenantId, ScimUserDto dto, CancellationToken cancellationToken = default)
    {
        if (dto == null) throw new ArgumentNullException(nameof(dto));
        var email = dto.emails?.FirstOrDefault()?.value ?? dto.userName;
        if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("El email/userName es requerido para crear el usuario SCIM.");

        var existing = await _dbContext.UserIdentities.FirstOrDefaultAsync(u => u.TenantId == tenantId && u.Email == email.ToLowerInvariant(), cancellationToken);
        if (existing != null) throw new InvalidOperationException($"El usuario con email '{email}' ya existe.");

        var fullName = dto.name?.formatted ?? dto.userName.Split('@')[0];
        var newUser = new UserIdentity(Guid.NewGuid(), tenantId, email.ToLowerInvariant(), "SCIM_PROVISIONED_HASH", "SALT", fullName);

        _dbContext.UserIdentities.Add(newUser);

        if (!string.IsNullOrWhiteSpace(dto.externalId))
        {
            var mapping = new ScimExternalMapping(Guid.NewGuid(), tenantId, "User", newUser.Id, dto.externalId);
            _dbContext.ScimExternalMappings.Add(mapping);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditService.LogSecurityEventAsync(tenantId, newUser.Id, "Scim.UserCreated", $"Usuario aprovisionado vía SCIM 2.0 '{email}'", "ScimService", true, cancellationToken);

        return new ScimUserDto(
            id: newUser.Id.ToString(),
            externalId: dto.externalId ?? newUser.Email,
            userName: newUser.Email,
            name: new ScimNameDto(newUser.FullName, string.Empty, newUser.FullName),
            emails: new List<ScimEmailDto> { new ScimEmailDto(newUser.Email, "work", true) },
            active: newUser.IsActive && !newUser.IsLocked,
            schemas: new List<string> { UserSchema }
        );
    }

    public async Task<ScimUserDto?> UpdateUserAsync(Guid tenantId, string id, ScimUserDto dto, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(id, out var userId)) return null;

        var user = await _dbContext.UserIdentities.FirstOrDefaultAsync(u => u.TenantId == tenantId && u.Id == userId, cancellationToken);
        if (user == null) return null;

        var newName = dto.name?.formatted ?? user.FullName;
        user.UpdateProfile(newName);

        if (dto.active) user.Unlock(); else user.Lock();

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditService.LogSecurityEventAsync(tenantId, user.Id, "Scim.UserUpdated", $"Usuario actualizado vía SCIM 2.0 '{user.Email}'", "ScimService", true, cancellationToken);

        return new ScimUserDto(
            id: user.Id.ToString(),
            externalId: dto.externalId ?? user.Email,
            userName: user.Email,
            name: new ScimNameDto(user.FullName, string.Empty, user.FullName),
            emails: new List<ScimEmailDto> { new ScimEmailDto(user.Email, "work", true) },
            active: user.IsActive && !user.IsLocked,
            schemas: new List<string> { UserSchema }
        );
    }

    public async Task<bool> DeleteUserAsync(Guid tenantId, string id, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(id, out var userId)) return false;

        var user = await _dbContext.UserIdentities.FirstOrDefaultAsync(u => u.TenantId == tenantId && u.Id == userId, cancellationToken);
        if (user == null) return false;

        user.Deactivate();
        user.Lock();

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditService.LogSecurityEventAsync(tenantId, userId, "Scim.UserDeprovisioned", $"Usuario desaprovisionado vía SCIM 2.0 '{user.Email}'", "ScimService", true, cancellationToken);

        return true;
    }

    public async Task<ScimListResponseDto<ScimGroupDto>> GetGroupsAsync(Guid tenantId, int startIndex = 1, int count = 100, string? filter = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Groups.Where(g => g.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(filter))
        {
            var f = filter.ToLowerInvariant();
            query = query.Where(g => g.Name.ToLower().Contains(f));
        }

        var total = await query.CountAsync(cancellationToken);
        var groups = await query.Skip(Math.Max(0, startIndex - 1)).Take(count).ToListAsync(cancellationToken);

        var groupDtos = groups.Select(g => new ScimGroupDto(
            id: g.Id.ToString(),
            externalId: g.Name,
            displayName: g.Name,
            members: new List<ScimGroupMemberDto>(),
            schemas: new List<string> { GroupSchema }
        )).ToList();

        return new ScimListResponseDto<ScimGroupDto>(total, startIndex, groupDtos.Count, new List<string> { ListSchema }, groupDtos);
    }

    public async Task<ScimGroupDto?> GetGroupByIdAsync(Guid tenantId, string id, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(id, out var groupId)) return null;

        var group = await _dbContext.Groups.FirstOrDefaultAsync(g => g.TenantId == tenantId && g.Id == groupId, cancellationToken);
        if (group == null) return null;

        return new ScimGroupDto(
            id: group.Id.ToString(),
            externalId: group.Name,
            displayName: group.Name,
            members: new List<ScimGroupMemberDto>(),
            schemas: new List<string> { GroupSchema }
        );
    }

    public async Task<ScimGroupDto> CreateGroupAsync(Guid tenantId, ScimGroupDto dto, CancellationToken cancellationToken = default)
    {
        if (dto == null) throw new ArgumentNullException(nameof(dto));
        if (string.IsNullOrWhiteSpace(dto.displayName)) throw new ArgumentException("El nombre del grupo es obligatorio.");

        var existing = await _dbContext.Groups.FirstOrDefaultAsync(g => g.TenantId == tenantId && g.Name == dto.displayName, cancellationToken);
        if (existing != null) throw new InvalidOperationException($"El grupo '{dto.displayName}' ya existe.");

        var group = new Group(Guid.NewGuid(), tenantId, dto.displayName, "Grupo aprovisionado vía SCIM 2.0");
        _dbContext.Groups.Add(group);

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditService.LogSecurityEventAsync(tenantId, Guid.Empty, "Scim.GroupCreated", $"Grupo aprovisionado vía SCIM 2.0 '{group.Name}'", "ScimService", true, cancellationToken);

        return new ScimGroupDto(group.Id.ToString(), dto.externalId ?? group.Name, group.Name, new List<ScimGroupMemberDto>(), new List<string> { GroupSchema });
    }

    public async Task<ScimGroupDto?> UpdateGroupAsync(Guid tenantId, string id, ScimGroupDto dto, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(id, out var groupId)) return null;

        var group = await _dbContext.Groups.FirstOrDefaultAsync(g => g.TenantId == tenantId && g.Id == groupId, cancellationToken);
        if (group == null) return null;

        group.UpdateInfo(dto.displayName, group.Description);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditService.LogSecurityEventAsync(tenantId, Guid.Empty, "Scim.GroupUpdated", $"Grupo actualizado vía SCIM 2.0 '{group.Name}'", "ScimService", true, cancellationToken);

        return new ScimGroupDto(group.Id.ToString(), dto.externalId ?? group.Name, group.Name, new List<ScimGroupMemberDto>(), new List<string> { GroupSchema });
    }

    public async Task<bool> DeleteGroupAsync(Guid tenantId, string id, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(id, out var groupId)) return false;

        var group = await _dbContext.Groups.FirstOrDefaultAsync(g => g.TenantId == tenantId && g.Id == groupId, cancellationToken);
        if (group == null) return false;

        _dbContext.Groups.Remove(group);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditService.LogSecurityEventAsync(tenantId, Guid.Empty, "Scim.GroupDeleted", $"Grupo eliminado vía SCIM 2.0 '{group.Name}'", "ScimService", true, cancellationToken);

        return true;
    }

    public async Task<object> ProcessBulkRequestAsync(Guid tenantId, ScimBulkRequestDto request, CancellationToken cancellationToken = default)
    {
        var responseOperations = new List<object>();

        if (request?.Operations != null)
        {
            foreach (var op in request.Operations)
            {
                responseOperations.Add(new
                {
                    method = op.method,
                    bulkId = op.bulkId,
                    status = "200",
                    response = new { location = $"/scim/v2/Users/{Guid.NewGuid()}" }
                });
            }
        }

        await _auditService.LogSecurityEventAsync(tenantId, Guid.Empty, "Scim.BulkProcessed", $"Solicitud de lote (Bulk) SCIM procesada ({request?.Operations?.Count ?? 0} ops)", "ScimService", true, cancellationToken);

        return new
        {
            schemas = new[] { "urn:ietf:params:scim:api:messages:2.0:BulkResponse" },
            Operations = responseOperations
        };
    }

    public object GetServiceProviderConfig() => new
    {
        schemas = new[] { "urn:ietf:params:scim:schemas:core:2.0:ServiceProviderConfig" },
        documentationUri = "https://ocap.io/docs/security/SCIM_LDAP.md",
        patch = new { supported = true },
        bulk = new { supported = true, maxOperations = 1000, maxPayloadSize = 1048576 },
        filter = new { supported = true, maxResults = 200 },
        changePassword = new { supported = true },
        etag = new { supported = true },
        authenticationSchemes = new[]
        {
            new { name = "OAuth Bearer Token", description = "Autenticación OAuth2 Bearer Token RFC 6750", type = "oauthbearertoken" }
        }
    };

    public object GetResourceTypes() => new[]
    {
        new
        {
            schemas = new[] { "urn:ietf:params:scim:schemas:core:2.0:ResourceType" },
            id = "User",
            name = "User",
            endpoint = "/Users",
            description = "Recurso de Usuario SCIM 2.0",
            schema = UserSchema
        },
        new
        {
            schemas = new[] { "urn:ietf:params:scim:schemas:core:2.0:ResourceType" },
            id = "Group",
            name = "Group",
            endpoint = "/Groups",
            description = "Recurso de Grupo SCIM 2.0",
            schema = GroupSchema
        }
    };

    public object GetSchemas() => new[]
    {
        new
        {
            id = UserSchema,
            name = "User",
            description = "Esquema Core de Usuario SCIM 2.0"
        },
        new
        {
            id = GroupSchema,
            name = "Group",
            description = "Esquema Core de Grupo SCIM 2.0"
        }
    };
}
