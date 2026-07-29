using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OCAP.Security.Domain.Entities;

namespace OCAP.Infrastructure.Persistence.Configurations;

public class ApiKeyConfiguration : IEntityTypeConfiguration<ApiKey>
{
    public void Configure(EntityTypeBuilder<ApiKey> builder)
    {
        builder.ToTable("ApiKeys");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.KeyHash).IsRequired().HasMaxLength(256);
        builder.Property(x => x.Prefix).IsRequired().HasMaxLength(32);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(128);
        builder.Property(x => x.Scopes).HasMaxLength(1000);
        builder.HasIndex(x => x.KeyHash).IsUnique();
        builder.HasIndex(x => x.TenantId);
    }
}

public class WebhookSubscriptionConfiguration : IEntityTypeConfiguration<WebhookSubscription>
{
    public void Configure(EntityTypeBuilder<WebhookSubscription> builder)
    {
        builder.ToTable("WebhookSubscriptions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(128);
        builder.Property(x => x.TargetUrl).IsRequired().HasMaxLength(2048);
        builder.Property(x => x.Secret).IsRequired().HasMaxLength(512);
        builder.Property(x => x.SubscribedEvents).HasMaxLength(2000);
        builder.HasIndex(x => x.TenantId);
    }
}

public class WebhookDeliveryLogConfiguration : IEntityTypeConfiguration<WebhookDeliveryLog>
{
    public void Configure(EntityTypeBuilder<WebhookDeliveryLog> builder)
    {
        builder.ToTable("WebhookDeliveryLogs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EventType).IsRequired().HasMaxLength(128);
        builder.Property(x => x.TargetUrl).IsRequired().HasMaxLength(2048);
        builder.Property(x => x.ResponseBody).HasMaxLength(4000);
        builder.Property(x => x.ErrorMessage).HasMaxLength(2000);
        builder.HasIndex(x => x.SubscriptionId);
        builder.HasIndex(x => x.TenantId);
    }
}
public class UserClaimConfiguration : IEntityTypeConfiguration<UserClaim>
{
    public void Configure(EntityTypeBuilder<UserClaim> builder)
    {
        builder.ToTable("UserClaims");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ClaimType).IsRequired().HasMaxLength(256);
        builder.Property(x => x.ClaimValue).IsRequired().HasMaxLength(1024);
        builder.HasIndex(x => new { x.UserId, x.TenantId });
    }
}

public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("UserRoles");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.UserId, x.RoleId, x.TenantId }).IsUnique();
        builder.HasIndex(x => x.TenantId);
    }
}

public class UserConsentConfiguration : IEntityTypeConfiguration<UserConsent>
{
    public void Configure(EntityTypeBuilder<UserConsent> builder)
    {
        builder.ToTable("UserConsents");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ClientId).IsRequired().HasMaxLength(256);
        builder.Property(x => x.GrantedScopes).HasMaxLength(2000);
        builder.HasIndex(x => new { x.UserId, x.ClientId, x.TenantId });
        builder.HasIndex(x => x.TenantId);
    }
}

public class GroupConfiguration : IEntityTypeConfiguration<Group>
{
    public void Configure(EntityTypeBuilder<Group> builder)
    {
        builder.ToTable("Groups");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(128);
        builder.Property(x => x.Description).HasMaxLength(512);
        builder.HasIndex(x => new { x.TenantId, x.Name }).IsUnique();
    }
}

public class UserGroupConfiguration : IEntityTypeConfiguration<UserGroup>
{
    public void Configure(EntityTypeBuilder<UserGroup> builder)
    {
        builder.ToTable("UserGroups");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.TenantId, x.UserId, x.GroupId }).IsUnique();
    }
}

public class GroupRoleConfiguration : IEntityTypeConfiguration<GroupRole>
{
    public void Configure(EntityTypeBuilder<GroupRole> builder)
    {
        builder.ToTable("GroupRoles");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.TenantId, x.GroupId, x.RoleId }).IsUnique();
    }
}

public class UserMfaSettingsConfiguration : IEntityTypeConfiguration<UserMfaSettings>
{
    public void Configure(EntityTypeBuilder<UserMfaSettings> builder)
    {
        builder.ToTable("UserMfaSettings");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.TenantId, x.UserId }).IsUnique();
        builder.Property(x => x.EncryptedTotpSecret).IsRequired().HasMaxLength(2000);
    }
}

public class UserRecoveryCodeConfiguration : IEntityTypeConfiguration<UserRecoveryCode>
{
    public void Configure(EntityTypeBuilder<UserRecoveryCode> builder)
    {
        builder.ToTable("UserRecoveryCodes");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.TenantId, x.UserId });
    }
}

public class WebAuthnCredentialConfiguration : IEntityTypeConfiguration<WebAuthnCredential>
{
    public void Configure(EntityTypeBuilder<WebAuthnCredential> builder)
    {
        builder.ToTable("WebAuthnCredentials");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.TenantId, x.CredentialId }).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.UserId });
        builder.Property(x => x.DeviceName).HasMaxLength(256);
    }
}


