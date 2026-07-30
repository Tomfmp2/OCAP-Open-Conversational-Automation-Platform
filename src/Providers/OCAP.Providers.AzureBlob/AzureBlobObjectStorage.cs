using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using OCAP.Core.Storage;

namespace OCAP.Providers.AzureBlob;

public sealed class AzureBlobObjectStorage : IObjectStorage
{
    private readonly BlobContainerClient _container;

    public AzureBlobObjectStorage(StorageOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            throw new ArgumentException("Storage:ConnectionString es obligatorio para Azure Blob.", nameof(options));
        }

        if (string.IsNullOrWhiteSpace(options.Container))
        {
            throw new ArgumentException("Storage:Container es obligatorio para Azure Blob.", nameof(options));
        }

        _container = new BlobContainerClient(options.ConnectionString, options.Container);
    }

    public AzureBlobObjectStorage(BlobContainerClient container)
    {
        _container = container ?? throw new ArgumentNullException(nameof(container));
    }

    public string ProviderName => "AzureBlob";

    public async Task UploadAsync(string path, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        await _container.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
        await _container.GetBlobClient(NormalizePath(path)).UploadAsync(
            content,
            new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType
                }
            },
            cancellationToken);
    }

    public async Task<Stream> DownloadAsync(string path, CancellationToken cancellationToken = default)
    {
        var response = await _container.GetBlobClient(NormalizePath(path)).DownloadStreamingAsync(cancellationToken: cancellationToken);
        var result = new MemoryStream();
        await using (response.Value.Content)
        {
            await response.Value.Content.CopyToAsync(result, cancellationToken);
        }

        result.Position = 0;
        return result;
    }

    public async Task DeleteAsync(string path, CancellationToken cancellationToken = default)
    {
        await _container.GetBlobClient(NormalizePath(path))
            .DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, cancellationToken: cancellationToken);
    }

    public async Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default) =>
        (await _container.GetBlobClient(NormalizePath(path)).ExistsAsync(cancellationToken)).Value;

    public async Task<IReadOnlyList<string>> ListAsync(string prefix, CancellationToken cancellationToken = default)
    {
        var results = new List<string>();
        await foreach (var blob in _container.GetBlobsAsync(
                           traits: BlobTraits.None,
                           states: BlobStates.None,
                           prefix: NormalizePath(prefix, allowEmpty: true),
                           cancellationToken: cancellationToken))
        {
            results.Add(blob.Name);
        }

        return results;
    }

    public async Task<bool> HealthAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return (await _container.ExistsAsync(cancellationToken)).Value;
        }
        catch (RequestFailedException)
        {
            return false;
        }
    }

    private static string NormalizePath(string path, bool allowEmpty = false)
    {
        var value = (path ?? string.Empty).Replace('\\', '/').TrimStart('/');
        if (!allowEmpty && string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("La ruta no puede estar vacía.", nameof(path));
        }

        return value;
    }
}
