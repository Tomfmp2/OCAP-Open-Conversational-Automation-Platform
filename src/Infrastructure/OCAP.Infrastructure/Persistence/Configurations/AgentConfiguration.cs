using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OCAP.Agents.Domain.Entities;
using OCAP.Agents.Domain.ValueObjects;

namespace OCAP.Infrastructure.Persistence.Configurations;

public class AgentConfiguration : IEntityTypeConfiguration<Agent>
{
    public void Configure(EntityTypeBuilder<Agent> builder)
    {
        builder.ToTable("Agents");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .HasConversion(
                v => v.Value,
                v => new AgentName(v))
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Description)
            .HasMaxLength(500);

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(50);

        builder.OwnsOne(x => x.Configuration, config =>
        {
            config.Property(c => c.SystemPrompt)
                .HasColumnName("SystemPrompt")
                .IsRequired();

            config.Property(c => c.Parameters)
                .HasColumnName("Parameters")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, string>());

            config.Property(c => c.AllowedToolNames)
                .HasColumnName("AllowedToolNames")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>());
        });
    }
}
