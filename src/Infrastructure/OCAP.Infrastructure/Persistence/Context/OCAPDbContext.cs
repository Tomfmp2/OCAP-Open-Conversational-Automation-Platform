using Microsoft.EntityFrameworkCore;
using OCAP.Core.Entities;

namespace OCAP.Infrastructure.Persistence.Context;

public class OCAPDbContext : DbContext
{
    public OCAPDbContext(DbContextOptions<OCAPDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<ToolExecution> ToolExecutions => Set<ToolExecution>();
    public DbSet<AgentToolPermission> AgentToolPermissions => Set<AgentToolPermission>();
    public DbSet<OAuthConnection> OAuthConnections => Set<OAuthConnection>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OCAPDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
