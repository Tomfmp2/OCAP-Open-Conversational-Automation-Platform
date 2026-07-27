using Microsoft.EntityFrameworkCore;
using OCAP.Core.Entities;
using OCAP.Infrastructure.Persistence.Context;

namespace OCAP.Infrastructure.Persistence.Repositories;

public class SessionRepository
{
    private readonly OCAPDbContext _context;

    public SessionRepository(OCAPDbContext context)
    {
        _context = context;
    }

    public async Task SaveAsync(Session session, CancellationToken cancellationToken = default)
    {
        var existing = await _context.Sessions.FindAsync(new object[] { session.Id }, cancellationToken);
        if (existing == null)
        {
            await _context.Sessions.AddAsync(session, cancellationToken);
        }
        else
        {
            _context.Entry(existing).CurrentValues.SetValues(session);
        }
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<Session?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Sessions.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }
}
