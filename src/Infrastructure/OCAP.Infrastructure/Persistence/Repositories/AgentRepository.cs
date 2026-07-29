using Microsoft.EntityFrameworkCore;
using OCAP.Agents.Abstractions.Ports;
using OCAP.Agents.Domain.Entities;
using OCAP.Infrastructure.Persistence.Context;

namespace OCAP.Infrastructure.Persistence.Repositories;

public class AgentRepository : IAgentRepository
{
    private readonly OCAPDbContext _context;

    public AgentRepository(OCAPDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<Agent?> GetByIdAsync(Guid agentId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Agent>().FirstOrDefaultAsync(a => a.Id == agentId, cancellationToken);
    }

    public async Task<Agent?> GetDefaultAgentAsync(CancellationToken cancellationToken = default)
    {
        // Simplest heuristic for default: get the first one available or a specific ID if known
        return await _context.Set<Agent>().FirstOrDefaultAsync(cancellationToken);
    }

    public async Task SaveAsync(Agent agent, CancellationToken cancellationToken = default)
    {
        var existing = await _context.Set<Agent>().FindAsync(new object[] { agent.Id }, cancellationToken);
        if (existing == null)
        {
            await _context.Set<Agent>().AddAsync(agent, cancellationToken);
        }
        else
        {
            _context.Entry(existing).CurrentValues.SetValues(agent);
        }
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IEnumerable<Agent>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<Agent>().ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Agent agent, CancellationToken cancellationToken = default)
    {
        await _context.Set<Agent>().AddAsync(agent, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Agent agent, CancellationToken cancellationToken = default)
    {
        _context.Set<Agent>().Update(agent);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Agent agent, CancellationToken cancellationToken = default)
    {
        _context.Set<Agent>().Remove(agent);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
