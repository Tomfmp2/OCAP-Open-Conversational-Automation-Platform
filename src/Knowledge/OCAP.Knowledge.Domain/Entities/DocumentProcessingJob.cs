using OCAP.Knowledge.Domain.Enums;

namespace OCAP.Knowledge.Domain.Entities;

public class DocumentProcessingJob
{
    public Guid Id { get; private set; }
    public Guid DocumentId { get; private set; }
    public Guid TenantId { get; private set; }
    public DocumentStatus Status { get; private set; }
    public string? ErrorMessage { get; private set; }
    public int ProgressPercentage { get; private set; }
    public DateTime StartedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }

    private DocumentProcessingJob() { }

    public DocumentProcessingJob(Guid id, Guid documentId, Guid tenantId)
    {
        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        DocumentId = documentId;
        TenantId = tenantId;
        Status = DocumentStatus.Processing;
        ProgressPercentage = 0;
        StartedAtUtc = DateTime.UtcNow;
    }

    public void UpdateProgress(int progressPercentage)
    {
        ProgressPercentage = Math.Clamp(progressPercentage, 0, 100);
        if (ProgressPercentage == 100)
        {
            Status = DocumentStatus.Indexed;
            CompletedAtUtc = DateTime.UtcNow;
        }
    }

    public void MarkFailed(string errorMessage)
    {
        Status = DocumentStatus.Failed;
        ErrorMessage = errorMessage;
        CompletedAtUtc = DateTime.UtcNow;
    }
}
