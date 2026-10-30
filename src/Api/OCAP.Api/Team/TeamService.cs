using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using OCAP.Api.Security;
using OCAP.Infrastructure.Persistence.Context;
using OCAP.Security.Abstractions;
using OCAP.Security.Domain.Entities;

namespace OCAP.Api.Team;

// Se envía como texto ("Admin" / "Operator") en la API y en el panel.
[System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter<TeamRole>))]
public enum TeamRole
{
    Admin,
    Operator
}

public sealed record TeamMemberDto(
    Guid Id,
    string Email,
    string FullName,
    TeamRole Role,
    bool IsActive,
    bool IsLocked,
    bool InvitationPending,
    bool MfaEnabled,
    DateTime CreatedAtUtc);

/// <summary>Enlace de un solo uso para definir la contraseña; Emailed indica si además salió por correo.</summary>
public sealed record PasswordLinkDto(string Path, DateTime ExpiresAtUtc, bool Emailed);

/// <summary>Error de una regla del equipo (último administrador, correo repetido…): 400 con el mensaje.</summary>
public sealed class TeamRuleException(string message) : Exception(message);

/// <summary>
/// Equipo de la organización: invitar (Admin u Operador), cambiar el rol, desactivar y generar enlaces para definir
/// la contraseña. Los enlaces sirven sin correo: el administrador los copia; con SMTP además se envían.
/// </summary>
public sealed class TeamService
{
    public const string AdminRoleName = "Admin";
    public const string OperatorRoleName = "Operador";
    private static readonly TimeSpan InvitationLifetime = TimeSpan.FromDays(7);
    private static readonly TimeSpan ResetLifetime = TimeSpan.FromHours(2);

    private readonly OCAPDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IEmailSender _email;
    private readonly ISecurityAuditService _audit;
    private readonly IConfiguration _configuration;

    public TeamService(OCAPDbContext db, IPasswordHasher hasher, IEmailSender email, ISecurityAuditService audit, IConfiguration configuration)
    {
        _db = db;
        _hasher = hasher;
        _email = email;
        _audit = audit;
        _configuration = configuration;
    }

    public Task<bool> IsEmailConfiguredAsync(Guid tenantId, CancellationToken cancellationToken) => _email.IsConfiguredAsync(tenantId, cancellationToken);

    /// <summary>Pantallas sin sesión (recuperar contraseña): no hay organización todavía.</summary>
    public Task<bool> IsAnyEmailConfiguredAsync(CancellationToken cancellationToken) => _email.IsAnyConfiguredAsync(cancellationToken);

    public async Task<IReadOnlyList<TeamMemberDto>> ListAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var users = await _db.UserIdentities.Where(u => u.TenantId == tenantId).OrderBy(u => u.CreatedAtUtc).ToListAsync(cancellationToken);
        var adminRoleIds = await AdminRoleIdsAsync(tenantId, cancellationToken);
        var roles = await _db.UserRoles.Where(r => r.TenantId == tenantId).ToListAsync(cancellationToken);
        var mfa = await _db.UserMfaSettings.Where(m => m.TenantId == tenantId && m.IsEnabled).Select(m => m.UserId).ToListAsync(cancellationToken);
        var now = DateTime.UtcNow;

        return users.Select(u => new TeamMemberDto(
                u.Id,
                u.Email,
                u.FullName,
                roles.Any(r => r.UserId == u.Id && adminRoleIds.Contains(r.RoleId)) ? TeamRole.Admin : TeamRole.Operator,
                u.IsActive,
                u.IsLockedOut(now),
                !u.IsEmailVerified && u.PasswordResetToken is not null,
                mfa.Contains(u.Id),
                u.CreatedAtUtc))
            .ToList();
    }

    public async Task<(TeamMemberDto Member, PasswordLinkDto Link)> InviteAsync(
        Guid tenantId, Guid actingUserId, string email, string fullName, TeamRole role, CancellationToken cancellationToken)
    {
        var normalized = email.Trim().ToLowerInvariant();
        // El login es solo por correo: un mismo correo en dos organizaciones sería ambiguo.
        if (await _db.UserIdentities.IgnoreQueryFilters().AnyAsync(u => u.Email == normalized, cancellationToken))
            throw new TeamRuleException("Ya existe una cuenta con ese correo.");

        // Contraseña aleatoria que nadie conoce: la persona define la suya con el enlace.
        var (hash, salt) = _hasher.HashPassword(Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
        var user = new UserIdentity(Guid.NewGuid(), tenantId, normalized, hash, salt, fullName.Trim());
        _db.UserIdentities.Add(user);
        var roleEntity = await EnsureRoleAsync(tenantId, role, cancellationToken);
        _db.UserRoles.Add(new UserRole(Guid.NewGuid(), user.Id, roleEntity.Id, tenantId));

        var link = await IssueLinkAsync(user, InvitationLifetime, isInvitation: true, cancellationToken);
        await _audit.LogSecurityEventAsync(tenantId, actingUserId, "Team.Invited", $"Invitación a {normalized} como {roleEntity.Name}", "TeamService", true, cancellationToken);

        return ((await ListAsync(tenantId, cancellationToken)).Single(m => m.Id == user.Id), link);
    }

    /// <summary>Nuevo enlace para definir la contraseña (olvido, invitación vencida). Invalida el anterior.</summary>
    public async Task<PasswordLinkDto?> CreatePasswordLinkAsync(Guid tenantId, Guid actingUserId, Guid userId, CancellationToken cancellationToken)
    {
        var user = await _db.UserIdentities.FirstOrDefaultAsync(u => u.TenantId == tenantId && u.Id == userId, cancellationToken);
        if (user is null) return null;

        var link = await IssueLinkAsync(user, user.IsEmailVerified ? ResetLifetime : InvitationLifetime, !user.IsEmailVerified, cancellationToken);
        await _audit.LogSecurityEventAsync(tenantId, actingUserId, "Team.PasswordLink", $"Enlace de contraseña para {user.Email}", "TeamService", true, cancellationToken);
        return link;
    }

    public async Task<bool> ChangeRoleAsync(Guid tenantId, Guid actingUserId, Guid userId, TeamRole role, CancellationToken cancellationToken)
    {
        var user = await _db.UserIdentities.FirstOrDefaultAsync(u => u.TenantId == tenantId && u.Id == userId, cancellationToken);
        if (user is null) return false;
        if (role == TeamRole.Operator) await EnsureAnotherAdminAsync(tenantId, actingUserId, userId, "quitarle el rol de administrador", cancellationToken);

        var target = await EnsureRoleAsync(tenantId, role, cancellationToken);
        _db.UserRoles.RemoveRange(await _db.UserRoles.Where(r => r.TenantId == tenantId && r.UserId == userId).ToListAsync(cancellationToken));
        _db.UserRoles.Add(new UserRole(Guid.NewGuid(), userId, target.Id, tenantId));
        await _db.SaveChangesAsync(cancellationToken);

        await _audit.LogSecurityEventAsync(tenantId, actingUserId, "Team.RoleChanged", $"{user.Email} ahora es {target.Name}", "TeamService", true, cancellationToken);
        return true;
    }

    public async Task<bool> SetActiveAsync(Guid tenantId, Guid actingUserId, Guid userId, bool active, CancellationToken cancellationToken)
    {
        var user = await _db.UserIdentities.FirstOrDefaultAsync(u => u.TenantId == tenantId && u.Id == userId, cancellationToken);
        if (user is null) return false;
        if (!active) await EnsureAnotherAdminAsync(tenantId, actingUserId, userId, "desactivarlo", cancellationToken);

        if (active) user.Activate();
        else user.Deactivate();
        await _db.SaveChangesAsync(cancellationToken);

        // Una cuenta desactivada no conserva sesiones abiertas.
        if (!active) await RevokeSessionsAsync(userId, cancellationToken);
        await _audit.LogSecurityEventAsync(tenantId, actingUserId, active ? "Team.Activated" : "Team.Deactivated", user.Email, "TeamService", true, cancellationToken);
        return true;
    }

    public async Task<bool> UnlockAsync(Guid tenantId, Guid actingUserId, Guid userId, CancellationToken cancellationToken)
    {
        var user = await _db.UserIdentities.FirstOrDefaultAsync(u => u.TenantId == tenantId && u.Id == userId, cancellationToken);
        if (user is null) return false;
        user.Unlock();
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.LogSecurityEventAsync(tenantId, actingUserId, "Team.Unlocked", user.Email, "TeamService", true, cancellationToken);
        return true;
    }

    /// <summary>Define la contraseña con un enlace (público). false si el enlace no existe, venció o ya se usó.</summary>
    public async Task<bool> CompletePasswordSetupAsync(string token, string newPassword, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token)) return false;
        var tokenHash = Hash(token);
        var user = await _db.UserIdentities.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.PasswordResetToken == tokenHash, cancellationToken);
        if (user is null || !user.IsActive || !user.HasValidPasswordSetup(tokenHash, DateTime.UtcNow)) return false;

        var (hash, salt) = _hasher.HashPassword(newPassword);
        user.CompletePasswordSetup(hash, salt);
        await _db.SaveChangesAsync(cancellationToken);
        // Si alguien más tenía la sesión (contraseña filtrada), deja de tenerla.
        await RevokeSessionsAsync(user.Id, cancellationToken);

        await _audit.LogSecurityEventAsync(user.TenantId, user.Id, "User.PasswordSet", "Contraseña definida con enlace", "TeamService", true, cancellationToken);
        return true;
    }

    /// <summary>
    /// "Olvidé mi contraseña": con correo configurado envía el enlace. La respuesta es la misma exista o no la
    /// cuenta, así no se puede averiguar qué correos están registrados.
    /// </summary>
    public async Task RequestPasswordResetAsync(string email, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(email)) return;
        var normalized = email.Trim().ToLowerInvariant();
        var user = await _db.UserIdentities.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Email == normalized && u.IsActive, cancellationToken);
        if (user is null || !await _email.IsConfiguredAsync(user.TenantId, cancellationToken)) return;

        await IssueLinkAsync(user, ResetLifetime, isInvitation: false, cancellationToken);
    }

    private async Task<PasswordLinkDto> IssueLinkAsync(UserIdentity user, TimeSpan lifetime, bool isInvitation, CancellationToken cancellationToken)
    {
        var token = Base64Url(RandomNumberGenerator.GetBytes(32));
        var expires = DateTime.UtcNow.Add(lifetime);
        user.IssuePasswordSetup(Hash(token), expires);
        await _db.SaveChangesAsync(cancellationToken);

        var path = $"/accept?token={token}";
        var emailed = false;
        // La dirección que eligió la organización en Ajustes o, si no, la del servidor.
        var preferences = await OCAP.Infrastructure.Services.TenantSettingsSections.ReadAsync<OCAP.Infrastructure.Services.OrganizationPreferences>(
            _db, user.TenantId, OCAP.Infrastructure.Services.OrganizationPreferences.Section, cancellationToken);
        var panelUrl = (preferences?.PanelUrl ?? _configuration["App:PublicPanelUrl"])?.TrimEnd('/');
        if (!string.IsNullOrWhiteSpace(panelUrl) && await _email.IsConfiguredAsync(user.TenantId, cancellationToken))
        {
            var subject = isInvitation ? "Te invitaron a OCAP" : "Restablece tu contraseña de OCAP";
            var intro = isInvitation
                ? "Te invitaron al panel de OCAP. Define tu contraseña para entrar:"
                : "Pediste restablecer tu contraseña de OCAP. Define una nueva aquí:";
            var body = $"{intro}\n\n{panelUrl}{path}\n\nEl enlace vence el {expires:yyyy-MM-dd HH:mm} UTC y sirve una sola vez.\nSi no lo esperabas, ignora este correo.";
            try
            {
                await _email.SendAsync(user.TenantId, user.Email, subject, body, cancellationToken);
                emailed = true;
            }
            catch (Exception ex) when (ex is System.Net.Mail.SmtpException or InvalidOperationException or System.Net.Sockets.SocketException)
            {
                // El enlace sigue siendo válido: el administrador puede copiarlo y compartirlo a mano.
            }
        }

        return new PasswordLinkDto(path, expires, emailed);
    }

    private async Task EnsureAnotherAdminAsync(Guid tenantId, Guid actingUserId, Guid userId, string action, CancellationToken cancellationToken)
    {
        if (userId == actingUserId) throw new TeamRuleException($"No puedes {action} a tu propia cuenta.");

        var adminRoleIds = await AdminRoleIdsAsync(tenantId, cancellationToken);
        var otherActiveAdmins = await (
            from ur in _db.UserRoles
            join u in _db.UserIdentities on ur.UserId equals u.Id
            where ur.TenantId == tenantId && adminRoleIds.Contains(ur.RoleId) && u.IsActive && u.Id != userId
            select u.Id).AnyAsync(cancellationToken);
        if (!otherActiveAdmins) throw new TeamRuleException($"Debe quedar al menos un administrador activo: no se puede {action}.");
    }

    private async Task<List<Guid>> AdminRoleIdsAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var roles = await _db.Roles.Where(r => r.TenantId == tenantId).ToListAsync(cancellationToken);
        return roles.Where(r => IsAdminRole(r)).Select(r => r.Id).ToList();
    }

    // Admin del provisionamiento inicial o cualquier rol que pueda administrar la seguridad de la organización.
    private static bool IsAdminRole(Role role) =>
        role.Name.Equals(AdminRoleName, StringComparison.OrdinalIgnoreCase)
        || role.Permissions.Contains(OcapPermissions.SecurityManage)
        || role.Permissions.Contains(OcapPermissions.FullAccess);

    private async Task<Role> EnsureRoleAsync(Guid tenantId, TeamRole role, CancellationToken cancellationToken)
    {
        var name = role == TeamRole.Admin ? AdminRoleName : OperatorRoleName;
        var existing = await _db.Roles.FirstOrDefaultAsync(r => r.TenantId == tenantId && r.Name == name, cancellationToken);
        if (existing is not null) return existing;

        var created = role == TeamRole.Admin
            ? new Role(Guid.NewGuid(), tenantId, AdminRoleName, "Administra la organización", OcapPermissions.All)
            : new Role(Guid.NewGuid(), tenantId, OperatorRoleName, "Atiende la bandeja y usa el asistente", OcapPermissions.Operator);
        _db.Roles.Add(created);
        await _db.SaveChangesAsync(cancellationToken);
        return created;
    }

    private async Task RevokeSessionsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var tokens = await _db.RefreshTokens.IgnoreQueryFilters().Where(t => t.UserId == userId && !t.IsRevoked).ToListAsync(cancellationToken);
        foreach (var token in tokens) token.Revoke();
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static string Hash(string token) => Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token))).ToLowerInvariant();

    private static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
