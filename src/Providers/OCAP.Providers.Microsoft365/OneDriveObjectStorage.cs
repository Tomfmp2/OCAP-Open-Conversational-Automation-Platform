using System.Net;
using System.Net.Http.Headers;
using OCAP.Core.Storage;
using System.Text.Json;

namespace OCAP.Providers.Microsoft365;

public sealed class OneDriveObjectStorage : IObjectStorage
{
    private const string DriveBase = "https://graph.microsoft.com/v1.0/me/drive";
    private readonly HttpClient _httpClient;
    private readonly string _accessToken;

    public OneDriveObjectStorage(HttpClient httpClient, StorageOptions options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        ArgumentNullException.ThrowIfNull(options);
        _accessToken = options.AccessToken;
    }

    public string ProviderName => "OneDrive";

    public async Task UploadAsync(string path, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        ArgumentNullException.ThrowIfNull(content);
        using var request = CreateRequest(HttpMethod.Put, ItemContentUri(path));
        request.Content = new NonDisposingStreamContent(content);
        request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(
            string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task<Stream> DownloadAsync(string path, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        using var request = CreateRequest(HttpMethod.Get, ItemContentUri(path));
        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        var result = new MemoryStream();
        await response.Content.CopyToAsync(result, cancellationToken);
        result.Position = 0;
        return result;
    }

    public async Task DeleteAsync(string path, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        using var request = CreateRequest(HttpMethod.Delete, ItemMetadataUri(path));
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode != HttpStatusCode.NotFound)
        {
            await EnsureSuccessAsync(response, cancellationToken);
        }
    }

    public async Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        using var request = CreateRequest(HttpMethod.Get, ItemMetadataUri(path) + "?$select=id");
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound) return false;
        await EnsureSuccessAsync(response, cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<string>> ListAsync(string prefix, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        var normalizedPrefix = NormalizePath(prefix, allowEmpty: true);
        var results = new List<string>();
        string? nextLink = $"{DriveBase}/root/delta?$select=name,file,parentReference,deleted";

        while (!string.IsNullOrWhiteSpace(nextLink))
        {
            using var request = CreateRequest(HttpMethod.Get, nextLink);
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var json = await ReadSuccessAsync(response, cancellationToken);
            using var document = JsonDocument.Parse(json);

            foreach (var item in document.RootElement.GetProperty("value").EnumerateArray())
            {
                if (!item.TryGetProperty("file", out _) || item.TryGetProperty("deleted", out _))
                {
                    continue;
                }

                var name = item.GetProperty("name").GetString() ?? string.Empty;
                var parentPath = item.TryGetProperty("parentReference", out var parent)
                    && parent.TryGetProperty("path", out var pathProperty)
                    ? pathProperty.GetString() ?? string.Empty
                    : string.Empty;
                const string rootMarker = "/drive/root:";
                var relativeParent = parentPath.StartsWith(rootMarker, StringComparison.OrdinalIgnoreCase)
                    ? parentPath[rootMarker.Length..].Trim('/')
                    : string.Empty;
                var objectPath = string.IsNullOrEmpty(relativeParent) ? name : $"{relativeParent}/{name}";
                if (objectPath.StartsWith(normalizedPrefix, StringComparison.Ordinal))
                {
                    results.Add(objectPath);
                }
            }

            nextLink = document.RootElement.TryGetProperty("@odata.nextLink", out var link)
                ? link.GetString()
                : null;
        }

        return results.Order(StringComparer.Ordinal).ToArray();
    }

    public async Task<bool> HealthAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_accessToken)) return false;
        try
        {
            using var request = CreateRequest(HttpMethod.Get, $"{DriveBase}/root?$select=id");
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string uri)
    {
        var request = new HttpRequestMessage(method, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
        return request;
    }

    private void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(_accessToken))
        {
            throw new InvalidOperationException("Storage:AccessToken es obligatorio para OneDrive.");
        }
    }

    private static string ItemMetadataUri(string path) =>
        $"{DriveBase}/root:/{EncodePath(path)}:";

    private static string ItemContentUri(string path) =>
        $"{ItemMetadataUri(path)}/content";

    private static string EncodePath(string path) =>
        string.Join('/', NormalizePath(path).Split('/').Select(Uri.EscapeDataString));

    private static string NormalizePath(string path, bool allowEmpty = false)
    {
        var value = (path ?? string.Empty).Replace('\\', '/').Trim('/');
        if (!allowEmpty && string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("La ruta no puede estar vacía.", nameof(path));
        }

        return value;
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"Microsoft Graph respondió {(int)response.StatusCode}: {body}", null, response.StatusCode);
        }
    }

    private static async Task<string> ReadSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    private sealed class NonDisposingStreamContent(Stream stream) : HttpContent
    {
        protected override Task SerializeToStreamAsync(Stream destination, TransportContext? context) =>
            stream.CopyToAsync(destination);

        protected override bool TryComputeLength(out long length)
        {
            if (stream.CanSeek)
            {
                length = stream.Length - stream.Position;
                return true;
            }

            length = 0;
            return false;
        }
    }
}
