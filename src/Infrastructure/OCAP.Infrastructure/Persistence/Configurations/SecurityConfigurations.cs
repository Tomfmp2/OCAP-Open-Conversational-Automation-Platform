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

