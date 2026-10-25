using Microsoft.EntityFrameworkCore;
using OCAP.Infrastructure.Persistence.Context;
using OCAP.Security.Abstractions;

namespace OCAP.Security.Infrastructure.Services;

public sealed class EfUserAuthenticationQuery : IUserAuthenticationQuery
{
    private readonly OCAPDbContext _dbContext;

    public EfUserAuthenticationQuery(OCAPDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<UserAuthenticationRecord?> FindByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email)) return null;

        var normalized = email.Trim().ToLowerInvariant();
        var user = await _dbContext.UserIdentities
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email.ToLower() == normalized && u.IsActive, cancellationToken);

        return user is null ? null : await BuildRecordAsync(user, cancellationToken);
    }

    public async Task<UserAuthenticationRecord?> FindByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.UserIdentities
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive, cancellationToken);

        return user is null ? null : await BuildRecordAsync(user, cancellationToken);
    }

    public Task<bool> IsMfaEnabledAsync(Guid userId, CancellationToken cancellationToken = default) =>
        _dbContext.UserMfaSettings
            .IgnoreQueryFilters()
            .AnyAsync(m => m.UserId == userId && m.IsEnabled, cancellationToken);

    private async Task<UserAuthenticationRecord?> BuildRecordAsync(OCAP.Security.Domain.Entities.UserIdentity user, CancellationToken cancellationToken)
    {
        var tenant = await _dbContext.Tenants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == user.TenantId, cancellationToken);
        if (tenant is null || !tenant.IsActive) return null;

        var userRole = await _dbContext.UserRoles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(ur => ur.UserId == user.Id, cancellationToken);

        if (userRole is null) return null;

        var role = await _dbContext.Roles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.Id == userRole.RoleId, cancellationToken);

        if (role is null) return null;

        return new UserAuthenticationRecord(user, tenant, role);
    }

    /// <summary>Contraseñas incorrectas seguidas que bloquean la cuenta.</summary>
    public const int MaxFailedAttempts = 5;
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    public async Task RecordLoginAttemptAsync(Guid userId, bool succeeded, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.UserIdentities
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null) return;

        if (succeeded) user.RegisterSuccessfulLogin();
        else user.RegisterFailedLogin(MaxFailedAttempts, LockoutDuration);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
