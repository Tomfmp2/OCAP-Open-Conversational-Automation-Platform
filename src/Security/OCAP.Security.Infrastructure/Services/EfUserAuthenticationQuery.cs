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

        if (user is null) return null;

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
}
