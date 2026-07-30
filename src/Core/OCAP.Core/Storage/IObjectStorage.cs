namespace OCAP.Core.Storage;

/// <summary>
/// Puerto de almacenamiento de objetos (local, S3, Azure Blob, Google Drive, OneDrive).
/// </summary>
public interface IObjectStorage
{
    string ProviderName { get; }

    Task UploadAsync(string path, Stream content, string contentType, CancellationToken cancellationToken = default);

    Task<Stream> DownloadAsync(string path, CancellationToken cancellationToken = default);

    Task DeleteAsync(string path, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> ListAsync(string prefix, CancellationToken cancellationToken = default);

    Task<bool> HealthAsync(CancellationToken cancellationToken = default);
}
