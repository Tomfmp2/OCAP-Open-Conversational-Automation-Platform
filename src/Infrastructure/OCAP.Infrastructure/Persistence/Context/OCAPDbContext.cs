using System.Reflection;
using Microsoft.EntityFrameworkCore;
using OCAP.Core.Entities;
using OCAP.Intelligence.Domain;
using OCAP.Security.Domain.Entities;
using OCAP.Workflow.Domain.Entities;
using OCAP.Knowledge.Domain.Entities;
using OCAP.Security.Abstractions;
using OCAP.Infrastructure.Persistence.Tenancy;

namespace OCAP.Infrastructure.Persistence.Context;

public class OCAPDbContext : DbContext
{
    private readonly ITenantContext _tenantContext;

    public OCAPDbContext(DbContextOptions<OCAPDbContext> options)
        : this(options, SystemTenantContext.Instance)
    {
    }

    public OCAPDbContext(DbContextOptions<OCAPDbContext> options, ITenantContext tenantContext) : base(options)
    {
        _tenantContext = tenantContext ?? SystemTenantContext.Instance;
    }

    /// <summary>
    /// Tenant activo capturado por los HasQueryFilter (evaluado por instancia de DbContext).
    /// </summary>
    public Guid CurrentTenantId => _tenantContext.TenantId;

    /// <summary>
    /// Indica si los filtros globales de tenant están desactivados para esta instancia.
    /// </summary>
    public bool BypassTenantFilters => _tenantContext.BypassTenantFilters;

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
    public DbSet<OCAP.Agents.Domain.Entities.Agent> Agents => Set<OCAP.Agents.Domain.Entities.Agent>();
    public DbSet<UserIdentity> UserIdentities => Set<UserIdentity>();
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<TenantMember> TenantMembers => Set<TenantMember>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<OCAP.Security.Domain.Entities.Permission> Permissions => Set<OCAP.Security.Domain.Entities.Permission>();
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
    public DbSet<LdapProviderConfig> LdapProviderConfigs => Set<LdapProviderConfig>();
    public DbSet<DirectorySyncJob> DirectorySyncJobs => Set<DirectorySyncJob>();
    public DbSet<DirectorySyncHistory> DirectorySyncHistories => Set<DirectorySyncHistory>();
    public DbSet<ScimExternalMapping> ScimExternalMappings => Set<ScimExternalMapping>();
    public DbSet<OCAP.Core.Events.Distributed.OutboxMessage> DistributedOutboxMessages => Set<OCAP.Core.Events.Distributed.OutboxMessage>();
    public DbSet<OCAP.Core.Events.Distributed.InboxMessage> InboxMessages => Set<OCAP.Core.Events.Distributed.InboxMessage>();
    public DbSet<OCAP.Core.Events.Distributed.DeadLetterMessage> DeadLetterMessages => Set<OCAP.Core.Events.Distributed.DeadLetterMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OCAPDbContext).Assembly);
        modelBuilder.UseOpenIddict();
        ApplyTenantQueryFilters(modelBuilder);
        base.OnModelCreating(modelBuilder);
    }

    private void ApplyTenantQueryFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (entityType.IsOwned()) continue;
            if (entityType.ClrType.Namespace?.StartsWith("OpenIddict", StringComparison.Ordinal) == true) continue;

            // Tenant catalog is global; access control is authorization, not row filter by TenantId column.
            if (entityType.ClrType == typeof(Tenant)) continue;

            // Global permission catalog and inbox idempotency store are intentionally not tenant-scoped.
            if (entityType.ClrType == typeof(OCAP.Security.Domain.Entities.Permission)) continue;
            if (entityType.ClrType == typeof(OCAP.Core.Events.Distributed.InboxMessage)) continue;

            var tenantProperty = entityType.FindProperty("TenantId");
            if (tenantProperty is null || tenantProperty.ClrType != typeof(Guid)) continue;

            var method = typeof(OCAPDbContext)
                .GetMethod(nameof(SetTenantFilter), BindingFlags.NonPublic | BindingFlags.Instance)!
                .MakeGenericMethod(entityType.ClrType);

            method.Invoke(this, [modelBuilder]);
        }
    }

    private void SetTenantFilter<TEntity>(ModelBuilder modelBuilder) where TEntity : class
    {
        modelBuilder.Entity<TEntity>().HasQueryFilter(e =>
            BypassTenantFilters || EF.Property<Guid>(e, "TenantId") == CurrentTenantId);
    }
}
