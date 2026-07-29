using Microsoft.EntityFrameworkCore;
using OCAP.Infrastructure.Persistence.Context;
using OCAP.Security.Abstractions;
using OCAP.Security.Abstractions.DTOs;
using OCAP.Security.Domain.Entities;

namespace OCAP.Security.Infrastructure.Services;

// Servicio de infraestructura para la gestión de usuarios, bloqueo, activación e invitación (CAP-16).
public class UserManagementService : IUserManagementService
{
    private readonly OCAPDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ISecurityAuditService _auditService;

    public UserManagementService(
        OCAPDbContext dbContext,
        IPasswordHasher passwordHasher,
        ISecurityAuditService auditService)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
    }

    public async Task<IReadOnlyList<UserDetailDto>> GetUsersAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserIdentities
            .AsNoTracking()
            .Where(u => u.TenantId == tenantId)
            .Select(u => new UserDetailDto(u.Id, u.TenantId, u.Email, u.FullName, u.IsActive, u.IsLocked, u.IsEmailVerified, u.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<UserDetailDto?> GetUserByIdAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.UserIdentities
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.TenantId == tenantId && u.Id == userId, cancellationToken);

        if (user == null) return null;

        return new UserDetailDto(user.Id, user.TenantId, user.Email, user.FullName, user.IsActive, user.IsLocked, user.IsEmailVerified, user.CreatedAtUtc);
    }

    public async Task<UserDetailDto> InviteUserAsync(Guid tenantId, InviteUserRequestDto request, CancellationToken cancellationToken = default)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Email))
            throw new ArgumentException("El correo electrónico es obligatorio.", nameof(request));

        var existing = await _dbContext.UserIdentities.FirstOrDefaultAsync(u => u.TenantId == tenantId && u.Email == request.Email.ToLowerInvariant(), cancellationToken);
        if (existing != null)
            throw new InvalidOperationException($"Ya existe un usuario registrado con el email '{request.Email}'.");

        var inviteToken = Guid.NewGuid().ToString("N");
        var tempPassword = Guid.NewGuid().ToString("N");
        var (hash, salt) = _passwordHasher.HashPassword(tempPassword);

        var newUser = new UserIdentity(Guid.NewGuid(), tenantId, request.Email, hash, salt, request.FullName, inviteToken);
        _dbContext.UserIdentities.Add(newUser);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditService.LogSecurityEventAsync(tenantId, newUser.Id, "User.Invited", $"Invitación enviada a '{request.Email}'", "UserManagementService", true, cancellationToken);

        return new UserDetailDto(newUser.Id, newUser.TenantId, newUser.Email, newUser.FullName, newUser.IsActive, newUser.IsLocked, newUser.IsEmailVerified, newUser.CreatedAtUtc);
    }

    public async Task<bool> LockUserAsync(Guid tenantId, Guid userId, TimeSpan? duration = null, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.UserIdentities.FirstOrDefaultAsync(u => u.TenantId == tenantId && u.Id == userId, cancellationToken);
        if (user == null || user.IsLocked) return false;

        user.Lock(duration);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditService.LogSecurityEventAsync(tenantId, userId, "User.Locked", $"Usuario {userId} bloqueado administrativamente", "UserManagementService", true, cancellationToken);
        return true;
    }

    public async Task<bool> UnlockUserAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.UserIdentities.FirstOrDefaultAsync(u => u.TenantId == tenantId && u.Id == userId, cancellationToken);
        if (user == null || !user.IsLocked) return false;

        user.Unlock();
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditService.LogSecurityEventAsync(tenantId, userId, "User.Unlocked", $"Usuario {userId} desbloqueado", "UserManagementService", true, cancellationToken);
        return true;
    }

    public async Task<bool> ActivateUserAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.UserIdentities.FirstOrDefaultAsync(u => u.TenantId == tenantId && u.Id == userId, cancellationToken);
        if (user == null || user.IsActive) return false;

        user.Activate();
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditService.LogSecurityEventAsync(tenantId, userId, "User.Activated", $"Usuario {userId} activado", "UserManagementService", true, cancellationToken);
        return true;
    }

    public async Task<bool> DeactivateUserAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.UserIdentities.FirstOrDefaultAsync(u => u.TenantId == tenantId && u.Id == userId, cancellationToken);
        if (user == null || !user.IsActive) return false;

        user.Deactivate();
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditService.LogSecurityEventAsync(tenantId, userId, "User.Deactivated", $"Usuario {userId} desactivado", "UserManagementService", true, cancellationToken);
        return true;
    }

    public async Task<bool> ResetPasswordAsync(Guid tenantId, ResetPasswordRequestDto request, CancellationToken cancellationToken = default)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Email)) return false;

        var user = await _dbContext.UserIdentities.FirstOrDefaultAsync(u => u.TenantId == tenantId && u.Email == request.Email.ToLowerInvariant(), cancellationToken);
        if (user == null) return false;

        var resetToken = Guid.NewGuid().ToString("N");
        user.SetPasswordResetToken(resetToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditService.LogSecurityEventAsync(tenantId, user.Id, "User.PasswordResetRequested", $"Token de restablecimiento generado para '{user.Email}'", "UserManagementService", true, cancellationToken);
        return true;
    }

    public async Task<bool> ChangePasswordAsync(Guid tenantId, Guid userId, ChangePasswordRequestDto request, CancellationToken cancellationToken = default)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.NewPassword)) return false;

        var user = await _dbContext.UserIdentities.FirstOrDefaultAsync(u => u.TenantId == tenantId && u.Id == userId, cancellationToken);
        if (user == null) return false;

        if (!_passwordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash, user.Salt))
        {
            await _auditService.LogSecurityEventAsync(tenantId, userId, "User.ChangePasswordFailed", "Contraseña actual incorrecta", "UserManagementService", false, cancellationToken);
            return false;
        }

        var (newHash, newSalt) = _passwordHasher.HashPassword(request.NewPassword);
        user.UpdatePassword(newHash, newSalt);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditService.LogSecurityEventAsync(tenantId, userId, "User.PasswordChanged", "Contraseña actualizada exitosamente", "UserManagementService", true, cancellationToken);
        return true;
    }

    public async Task<bool> VerifyEmailAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.UserIdentities.FirstOrDefaultAsync(u => u.TenantId == tenantId && u.Id == userId, cancellationToken);
        if (user == null || user.IsEmailVerified) return false;

        user.VerifyEmail();
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditService.LogSecurityEventAsync(tenantId, userId, "User.EmailVerified", "Correo electrónico verificado", "UserManagementService", true, cancellationToken);
        return true;
    }
}
