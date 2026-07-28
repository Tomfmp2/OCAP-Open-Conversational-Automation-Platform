namespace OCAP.Infrastructure.Options;

/// <summary>
/// Opciones de configuración para el servicio de retención y purga de auditoría y Outbox.
/// Implementa "Global Retention Policy v1.6.0" con capacidad de futura extensión a políticas por Tenant.
/// </summary>
public class RetentionOptions
{
    public const string SectionName = "Retention";

    public int AuditLogRetentionDays { get; set; } = 30;
    public int OutboxRetentionDays { get; set; } = 7;
    public int BatchSize { get; set; } = 500;
    public int ExecutionIntervalHours { get; set; } = 24;
    public bool EnableAuditPurge { get; set; } = true;
    public bool EnableOutboxPurge { get; set; } = true;
}
