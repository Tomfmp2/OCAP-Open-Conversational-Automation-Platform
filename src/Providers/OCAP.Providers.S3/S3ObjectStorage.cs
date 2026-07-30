using System.Net;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using OCAP.Core.Storage;

namespace OCAP.Providers.S3;

public sealed class S3ObjectStorage : IObjectStorage, IDisposable
{
    private readonly IAmazonS3 _client;
    private readonly string _bucket;
    private readonly bool _ownsClient;

    public S3ObjectStorage(StorageOptions options)
        : this(CreateClient(options), options?.Bucket ?? string.Empty, ownsClient: true)
    {
    }

    public S3ObjectStorage(IAmazonS3 client, string bucket, bool ownsClient = false)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _bucket = string.IsNullOrWhiteSpace(bucket)
            ? throw new ArgumentException("Storage:Bucket es obligatorio para S3.", nameof(bucket))
            : bucket;
        _ownsClient = ownsClient;
    }

    public string ProviderName => "S3";

    public async Task UploadAsync(string path, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        await _client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _bucket,
            Key = NormalizeKey(path),
            InputStream = content,
            ContentType = contentType,
            AutoCloseStream = false
        }, cancellationToken);
    }

    public async Task<Stream> DownloadAsync(string path, CancellationToken cancellationToken = default)
    {
        var response = await _client.GetObjectAsync(_bucket, NormalizeKey(path), cancellationToken);
        var copy = new MemoryStream();
        using (response)
        {
            await response.ResponseStream.CopyToAsync(copy, cancellationToken);
        }

        copy.Position = 0;
        return copy;
    }

    public Task DeleteAsync(string path, CancellationToken cancellationToken = default) =>
        _client.DeleteObjectAsync(_bucket, NormalizeKey(path), cancellationToken);

    public async Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default)
    {
        try
        {
            await _client.GetObjectMetadataAsync(_bucket, NormalizeKey(path), cancellationToken);
            return true;
        }
        catch (AmazonS3Exception exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public async Task<IReadOnlyList<string>> ListAsync(string prefix, CancellationToken cancellationToken = default)
    {
        var results = new List<string>();
        string? continuationToken = null;
        do
        {
            var response = await _client.ListObjectsV2Async(new ListObjectsV2Request
            {
                BucketName = _bucket,
                Prefix = NormalizeKey(prefix, allowEmpty: true),
                ContinuationToken = continuationToken
            }, cancellationToken);
            results.AddRange(response.S3Objects.Select(item => item.Key));
            continuationToken = response.IsTruncated == true ? response.NextContinuationToken : null;
        } while (continuationToken is not null);

        return results;
    }

    public async Task<bool> HealthAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _client.GetBucketLocationAsync(new GetBucketLocationRequest { BucketName = _bucket }, cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void Dispose()
    {
        if (_ownsClient)
        {
            _client.Dispose();
        }
    }

    private static IAmazonS3 CreateClient(StorageOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (string.IsNullOrWhiteSpace(options.AccessKey) || string.IsNullOrWhiteSpace(options.SecretKey))
        {
            throw new ArgumentException("Storage:AccessKey y Storage:SecretKey son obligatorios para S3.");
        }

        var config = new AmazonS3Config
        {
            RegionEndpoint = RegionEndpoint.GetBySystemName(
                string.IsNullOrWhiteSpace(options.Region) ? "us-east-1" : options.Region)
        };

        if (!string.IsNullOrWhiteSpace(options.ServiceUrl))
        {
            config.ServiceURL = options.ServiceUrl;
            config.ForcePathStyle = true;
            config.AuthenticationRegion = string.IsNullOrWhiteSpace(options.Region) ? "us-east-1" : options.Region;
        }

        return new AmazonS3Client(
            new BasicAWSCredentials(options.AccessKey, options.SecretKey),
            config);
    }

    private static string NormalizeKey(string path, bool allowEmpty = false)
    {
        var key = (path ?? string.Empty).Replace('\\', '/').TrimStart('/');
        if (!allowEmpty && string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("La ruta no puede estar vacía.", nameof(path));
        }

        return key;
    }
}
