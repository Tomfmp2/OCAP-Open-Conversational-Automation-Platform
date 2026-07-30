using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OCAP.Core.Entities;
using OCAP.Intelligence.Domain;
using OCAP.Security.Domain.Entities;
using OCAP.Workflow.Domain.Entities;
using OCAP.Workflow.Domain.Enums;

namespace OCAP.Infrastructure.Persistence.Configurations;

public class ToolExecutionConfiguration : IEntityTypeConfiguration<ToolExecution>
{
    public void Configure(EntityTypeBuilder<ToolExecution> builder)
    {
        builder.ToTable("ToolExecutions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ToolName).IsRequired().HasMaxLength(128);
        builder.Property(x => x.ErrorCode).HasMaxLength(128);
        builder.HasIndex(x => x.AgentId);
        builder.HasIndex(x => x.ConversationId);
        builder.HasIndex(x => x.ExecutedAt);
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => new { x.TenantId, x.ExecutedAt });
    }
}

public class AgentToolPermissionConfiguration : IEntityTypeConfiguration<AgentToolPermission>
{
    public void Configure(EntityTypeBuilder<AgentToolPermission> builder)
    {
        builder.ToTable("AgentToolPermissions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.PermissionName).IsRequired().HasMaxLength(128);
        builder.HasIndex(x => new { x.TenantId, x.AgentId, x.PermissionName }).IsUnique();
        builder.HasIndex(x => x.TenantId);
    }
}

public class OAuthConnectionConfiguration : IEntityTypeConfiguration<OAuthConnection>
{
    public void Configure(EntityTypeBuilder<OAuthConnection> builder)
    {
        builder.ToTable("OAuthConnections");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Provider).IsRequired().HasMaxLength(64);
        builder.Property(x => x.AccessToken).IsRequired();
        builder.Property(x => x.RefreshToken).IsRequired();
        builder.Property(x => x.Scopes).HasMaxLength(2000);
        builder.HasIndex(x => new { x.TenantId, x.UserId, x.Provider }).IsUnique();
        builder.HasIndex(x => x.TenantId);
    }
}

public class CoreOutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Type).IsRequired().HasMaxLength(256);
        builder.Property(x => x.Content).IsRequired();
        builder.Property(x => x.Error).HasMaxLength(4000);
        builder.HasIndex(x => x.ProcessedOnUtc);
        builder.HasIndex(x => x.OccurredOnUtc);
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => new { x.TenantId, x.ProcessedOnUtc });
    }
}

public class AiConversationMemoryConfiguration : IEntityTypeConfiguration<AiConversationMemory>
{
    public void Configure(EntityTypeBuilder<AiConversationMemory> builder)
    {
        builder.ToTable("AiConversationMemories");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.MemoryType).IsRequired().HasMaxLength(64);
        builder.Property(x => x.Content).IsRequired();
        builder.HasIndex(x => x.ConversationId);
        builder.HasIndex(x => x.CreatedAt);
        builder.HasIndex(x => new { x.TenantId, x.ConversationId });
    }
}

public class AiExecutionLogConfiguration : IEntityTypeConfiguration<AiExecutionLog>
{
    public void Configure(EntityTypeBuilder<AiExecutionLog> builder)
    {
        builder.ToTable("AiExecutionLogs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Provider).IsRequired().HasMaxLength(64);
        builder.Property(x => x.Model).IsRequired().HasMaxLength(128);
        builder.HasIndex(x => x.ExecutedAt);
        builder.HasIndex(x => x.Provider);
        builder.HasIndex(x => new { x.TenantId, x.ExecutedAt });
    }
}

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("Tenants");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(256);
        builder.Property(x => x.Slug).IsRequired().HasMaxLength(128);
        builder.Property(x => x.SettingsJson).HasColumnType("jsonb");
        builder.HasIndex(x => x.Slug).IsUnique();
    }
}

public class UserIdentityConfiguration : IEntityTypeConfiguration<UserIdentity>
{
    public void Configure(EntityTypeBuilder<UserIdentity> builder)
    {
        builder.ToTable("UserIdentities");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Email).IsRequired().HasMaxLength(320);
        builder.Property(x => x.PasswordHash).IsRequired().HasMaxLength(512);
        builder.Property(x => x.Salt).IsRequired().HasMaxLength(256);
        builder.Property(x => x.FullName).IsRequired().HasMaxLength(256);
        builder.Property(x => x.PasswordResetToken).HasMaxLength(512);
        builder.Property(x => x.InviteToken).HasMaxLength(512);
        builder.HasIndex(x => new { x.TenantId, x.Email }).IsUnique();
        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(128);
        builder.Property(x => x.Description).HasMaxLength(512);

        var permissionsComparer = new ValueComparer<List<string>>(
            (a, b) => (a ?? new List<string>()).SequenceEqual(b ?? new List<string>()),
            v => v.Aggregate(0, (hash, item) => HashCode.Combine(hash, item.GetHashCode())),
            v => v.ToList());

        builder.Property(x => x.Permissions)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
            .HasColumnType("jsonb")
            .Metadata.SetValueComparer(permissionsComparer);

        builder.HasIndex(x => new { x.TenantId, x.Name }).IsUnique();
        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("Permissions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).IsRequired().HasMaxLength(128);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(256);
        builder.Property(x => x.Category).IsRequired().HasMaxLength(128);
        builder.Property(x => x.Description).HasMaxLength(512);
        builder.HasIndex(x => x.Code).IsUnique();
    }
}

public class TenantMemberConfiguration : IEntityTypeConfiguration<TenantMember>
{
    public void Configure(EntityTypeBuilder<TenantMember> builder)
    {
        builder.ToTable("TenantMembers");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.TenantId, x.UserId }).IsUnique();
        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserIdentity>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Role>()
            .WithMany()
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Token).IsRequired().HasMaxLength(512);
        builder.Property(x => x.ReplacedByToken).HasMaxLength(512);
        builder.Ignore(x => x.IsExpired);
        builder.Ignore(x => x.IsActive);
        builder.HasIndex(x => x.Token).IsUnique();
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => new { x.TenantId, x.UserId });
        builder.HasOne<UserIdentity>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class UserSessionConfiguration : IEntityTypeConfiguration<UserSession>
{
    public void Configure(EntityTypeBuilder<UserSession> builder)
    {
        builder.ToTable("UserSessions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.IpAddress).HasMaxLength(64);
        builder.Property(x => x.UserAgent).HasMaxLength(512);
        builder.HasIndex(x => new { x.TenantId, x.UserId });
        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserIdentity>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Action).IsRequired().HasMaxLength(128);
        builder.Property(x => x.Details).HasMaxLength(4000);
        builder.Property(x => x.IpAddress).HasMaxLength(64);
        builder.HasIndex(x => new { x.TenantId, x.TimestampUtc });
        builder.HasIndex(x => x.UserId);
        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class WorkflowDefinitionConfiguration : IEntityTypeConfiguration<WorkflowDefinition>
{
    public void Configure(EntityTypeBuilder<WorkflowDefinition> builder)
    {
        builder.ToTable("WorkflowDefinitions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(256);
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
        builder.HasIndex(x => new { x.TenantId, x.Name });

        builder.OwnsMany(x => x.Steps, step =>
        {
            step.ToTable("WorkflowDefinitionSteps");
            step.WithOwner().HasForeignKey("WorkflowDefinitionId");
            step.HasKey(s => s.Id);
            step.Property(s => s.StepId).IsRequired().HasMaxLength(128);
            step.Property(s => s.Name).IsRequired().HasMaxLength(256);
            step.Property(s => s.NodeType).HasConversion<string>().HasMaxLength(64);
            step.Property(s => s.ConfigurationJson).HasColumnType("jsonb");
            step.HasIndex("WorkflowDefinitionId", nameof(WorkflowStep.StepId)).IsUnique();
        });

        builder.OwnsMany(x => x.Transitions, transition =>
        {
            transition.ToTable("WorkflowDefinitionTransitions");
            transition.WithOwner().HasForeignKey("WorkflowDefinitionId");
            transition.HasKey(t => t.Id);
            transition.Property(t => t.FromStepId).IsRequired().HasMaxLength(128);
            transition.Property(t => t.ToStepId).IsRequired().HasMaxLength(128);
            transition.Property(t => t.ConditionExpression).HasMaxLength(2000);
            transition.HasIndex("WorkflowDefinitionId");
        });

        builder.Navigation(x => x.Steps).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(x => x.Transitions).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public class WorkflowVersionConfiguration : IEntityTypeConfiguration<WorkflowVersion>
{
    public void Configure(EntityTypeBuilder<WorkflowVersion> builder)
    {
        builder.ToTable("WorkflowVersions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.DefinitionJson).IsRequired().HasColumnType("jsonb");
        builder.HasIndex(x => new { x.WorkflowDefinitionId, x.VersionNumber }).IsUnique();
        builder.HasIndex(x => x.TenantId);
        builder.HasOne<WorkflowDefinition>()
            .WithMany()
            .HasForeignKey(x => x.WorkflowDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class WorkflowExecutionConfiguration : IEntityTypeConfiguration<WorkflowExecution>
{
    public void Configure(EntityTypeBuilder<WorkflowExecution> builder)
    {
        builder.ToTable("WorkflowExecutions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CurrentStepId).HasMaxLength(128);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.OutputJson).HasColumnType("jsonb");
        builder.Property(x => x.ErrorMessage).HasMaxLength(4000);
        builder.HasIndex(x => new { x.TenantId, x.StartedAtUtc });
        builder.HasIndex(x => x.WorkflowDefinitionId);
        builder.HasOne<WorkflowDefinition>()
            .WithMany()
            .HasForeignKey(x => x.WorkflowDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class WorkflowExecutionHistoryConfiguration : IEntityTypeConfiguration<WorkflowExecutionHistory>
{
    public void Configure(EntityTypeBuilder<WorkflowExecutionHistory> builder)
    {
        builder.ToTable("WorkflowExecutionHistories");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.StepId).IsRequired().HasMaxLength(128);
        builder.Property(x => x.StepName).IsRequired().HasMaxLength(256);
        builder.Property(x => x.NodeType).IsRequired().HasMaxLength(64);
        builder.Property(x => x.Status).IsRequired().HasMaxLength(50);
        builder.Property(x => x.InputJson).HasColumnType("jsonb");
        builder.Property(x => x.OutputJson).HasColumnType("jsonb");
        builder.Property(x => x.ErrorMessage).HasMaxLength(4000);
        builder.HasIndex(x => x.ExecutionId);
        builder.HasIndex(x => new { x.TenantId, x.ExecutionId });
        builder.HasOne<WorkflowExecution>()
            .WithMany()
            .HasForeignKey(x => x.ExecutionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class WorkflowVariableConfiguration : IEntityTypeConfiguration<WorkflowVariable>
{
    public void Configure(EntityTypeBuilder<WorkflowVariable> builder)
    {
        builder.ToTable("WorkflowVariables");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Key).IsRequired().HasMaxLength(128);
        builder.Property(x => x.ValueJson).IsRequired().HasColumnType("jsonb");
        builder.HasIndex(x => new { x.ExecutionId, x.Key }).IsUnique();
        builder.HasIndex(x => x.TenantId);
        builder.HasOne<WorkflowExecution>()
            .WithMany()
            .HasForeignKey(x => x.ExecutionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
