namespace OCAP.Knowledge.Domain.Entities;

public class DocumentPermission
{
    public Guid Id { get; private set; }
    public Guid DocumentId { get; private set; }
    public Guid TenantId { get; private set; }
    public string Role { get; private set; } = string.Empty;
    public bool CanRead { get; private set; }
    public bool CanWrite { get; private set; }
    public bool CanDelete { get; private set; }

    private DocumentPermission() { }

    public DocumentPermission(
        Guid id,
        Guid documentId,
        Guid tenantId,
        string role,
        bool canRead = true,
        bool canWrite = false,
        bool canDelete = false)
    {
        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        DocumentId = documentId;
        TenantId = tenantId;
        Role = role ?? string.Empty;
        CanRead = canRead;
        CanWrite = canWrite;
        CanDelete = canDelete;
    }
}
