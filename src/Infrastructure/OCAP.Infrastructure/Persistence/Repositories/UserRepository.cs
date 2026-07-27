using Microsoft.EntityFrameworkCore;
using OCAP.Core.Entities;
using OCAP.Core.Ports;
using OCAP.Infrastructure.Persistence.Context;

namespace OCAP.Infrastructure.Persistence.Repositories;

public class UserRepository : IUserRepository
{
    private readonly OCAPDbContext _context;

    public UserRepository(OCAPDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }
}
