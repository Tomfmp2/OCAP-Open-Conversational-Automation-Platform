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
    public DbSet<AiProviderConfiguration> AiProviderConfigurations => Set<AiProviderConfiguration>();
    public DbSet<UserIdentity> UserIdentities => Set<UserIdentity>();
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<TenantMember> TenantMembers => Set<TenantMember>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<UserClaim> UserClaims => Set<UserClaim>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
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
    public DbSet<ExternalIdentity> ExternalIdentities => Set<ExternalIdentity>();
    public DbSet<ChannelConnection> ChannelConnections => Set<ChannelConnection>();
    public DbSet<WebhookSubscription> WebhookSubscriptions => Set<WebhookSubscription>();
    public DbSet<WebhookDeliveryLog> WebhookDeliveryLogs => Set<WebhookDeliveryLog>();
    public DbSet<UserConsent> UserConsents => Set<UserConsent>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<UserGroup> UserGroups => Set<UserGroup>();
    public DbSet<GroupRole> GroupRoles => Set<GroupRole>();
    public DbSet<UserMfaSettings> UserMfaSettings => Set<UserMfaSettings>();
    public DbSet<UserRecoveryCode> UserRecoveryCodes => Set<UserRecoveryCode>();
    public DbSet<WebAuthnCredential> WebAuthnCredentials => Set<WebAuthnCredential>();
    public DbSet<SamlProviderConfig> SamlProviderConfigs => Set<SamlProviderConfig>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OCAPDbContext).Assembly);
        modelBuilder.UseOpenIddict();
        base.OnModelCreating(modelBuilder);
    }
}
