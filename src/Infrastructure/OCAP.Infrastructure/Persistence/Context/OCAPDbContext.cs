using Microsoft.EntityFrameworkCore;
using OCAP.Core.Entities;
using OCAP.Intelligence.Domain;
using OCAP.Security.Domain.Entities;
using OCAP.Workflow.Domain.Entities;
using OCAP.Knowledge.Domain.Entities;

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
    public DbSet<AiConversationMemory> AiConversationMemories => Set<AiConversationMemory>();
    public DbSet<AiExecutionLog> AiExecutionLogs => Set<AiExecutionLog>();
    public DbSet<UserIdentity> UserIdentities => Set<UserIdentity>();
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<TenantMember> TenantMembers => Set<TenantMember>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<WorkflowDefinition> WorkflowDefinitions => Set<WorkflowDefinition>();
    public DbSet<WorkflowVersion> WorkflowVersions => Set<WorkflowVersion>();
    public DbSet<WorkflowExecution> WorkflowExecutions => Set<WorkflowExecution>();
    public DbSet<WorkflowExecutionHistory> WorkflowExecutionHistories => Set<WorkflowExecutionHistory>();
    public DbSet<WorkflowVariable> WorkflowVariables => Set<WorkflowVariable>();
    public DbSet<KnowledgeBase> KnowledgeBases => Set<KnowledgeBase>();
    public DbSet<KnowledgeDocument> KnowledgeDocuments => Set<KnowledgeDocument>();
    public DbSet<KnowledgeChunk> KnowledgeChunks => Set<KnowledgeChunk>();
    public DbSet<DocumentProcessingJob> DocumentProcessingJobs => Set<DocumentProcessingJob>();
    public DbSet<DocumentPermission> DocumentPermissions => Set<DocumentPermission>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OCAPDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
