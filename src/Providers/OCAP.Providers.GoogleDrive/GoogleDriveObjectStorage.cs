using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using OCAP.Core.Storage;

namespace OCAP.Providers.GoogleDrive;

public sealed class GoogleDriveObjectStorage : IObjectStorage
{
    private const string ApiBase = "https://www.googleapis.com/drive/v3";
    private const string UploadBase = "https://www.googleapis.com/upload/drive/v3";
    private readonly HttpClient _httpClient;
    private readonly string _accessToken;

    public GoogleDriveObjectStorage(HttpClient httpClient, StorageOptions options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        ArgumentNullException.ThrowIfNull(options);
        _accessToken = options.AccessToken;
    }

    public string ProviderName => "GoogleDrive";

    public async Task UploadAsync(string path, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        ArgumentNullException.ThrowIfNull(content);
        var normalizedPath = NormalizePath(path);
        var existingId = await FindFileIdAsync(normalizedPath, cancellationToken);
        var metadata = JsonSerializer.Serialize(new
        {
            name = Path.GetFileName(normalizedPath),
            appProperties = new Dictionary<string, string> { ["ocapPath"] = normalizedPath }
        });

        using var multipart = new MultipartContent("related");
        multipart.Add(new StringContent(metadata, Encoding.UTF8, "application/json"));
        var streamContent = new NonDisposingStreamContent(content);
        streamContent.Headers.ContentType = MediaTypeHeaderValue.Parse(
            string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType);
        multipart.Add(streamContent);

        var uri = existingId is null
            ? $"{UploadBase}/files?uploadType=multipart"
            : $"{UploadBase}/files/{Uri.EscapeDataString(existingId)}?uploadType=multipart";
        using var request = CreateRequest(existingId is null ? HttpMethod.Post : HttpMethod.Patch, uri);
        request.Content = multipart;
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task<Stream> DownloadAsync(string path, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        var id = await RequireFileIdAsync(path, cancellationToken);
        using var request = CreateRequest(HttpMethod.Get, $"{ApiBase}/files/{Uri.EscapeDataString(id)}?alt=media");
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
        var id = await FindFileIdAsync(NormalizePath(path), cancellationToken);
        if (id is null) return;

        using var request = CreateRequest(HttpMethod.Delete, $"{ApiBase}/files/{Uri.EscapeDataString(id)}");
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        return await FindFileIdAsync(NormalizePath(path), cancellationToken) is not null;
    }

    public async Task<IReadOnlyList<string>> ListAsync(string prefix, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        var normalizedPrefix = NormalizePath(prefix, allowEmpty: true);
        var results = new List<string>();
        string? pageToken = null;

        do
        {
            var uri = $"{ApiBase}/files?pageSize=1000&fields=nextPageToken,files(appProperties)"
                + "&q=" + Uri.EscapeDataString("trashed = false")
                + (pageToken is null ? string.Empty : "&pageToken=" + Uri.EscapeDataString(pageToken));
            using var request = CreateRequest(HttpMethod.Get, uri);
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var json = await ReadSuccessAsync(response, cancellationToken);
            using var document = JsonDocument.Parse(json);

            foreach (var file in document.RootElement.GetProperty("files").EnumerateArray())
            {
                if (file.TryGetProperty("appProperties", out var properties)
                    && properties.TryGetProperty("ocapPath", out var pathProperty)
                    && pathProperty.GetString() is { } storedPath
                    && storedPath.StartsWith(normalizedPrefix, StringComparison.Ordinal))
                {
                    results.Add(storedPath);
                }
            }

            pageToken = document.RootElement.TryGetProperty("nextPageToken", out var token)
                ? token.GetString()
                : null;
        } while (!string.IsNullOrWhiteSpace(pageToken));

        return results.Order(StringComparer.Ordinal).ToArray();
    }

    public async Task<bool> HealthAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_accessToken)) return false;
        try
        {
            using var request = CreateRequest(HttpMethod.Get, $"{ApiBase}/about?fields=user");
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private async Task<string?> FindFileIdAsync(string path, CancellationToken cancellationToken)
    {
        var escaped = path.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("'", "\\'", StringComparison.Ordinal);
        var query = $"trashed = false and appProperties has {{ key='ocapPath' and value='{escaped}' }}";
        var uri = $"{ApiBase}/files?pageSize=1&fields=files(id)&q={Uri.EscapeDataString(query)}";
        using var request = CreateRequest(HttpMethod.Get, uri);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var json = await ReadSuccessAsync(response, cancellationToken);
        using var document = JsonDocument.Parse(json);
        var files = document.RootElement.GetProperty("files");
        return files.GetArrayLength() == 0 ? null : files[0].GetProperty("id").GetString();
    }

    private async Task<string> RequireFileIdAsync(string path, CancellationToken cancellationToken)
    {
        var normalizedPath = NormalizePath(path);
        return await FindFileIdAsync(normalizedPath, cancellationToken)
            ?? throw new FileNotFoundException($"No existe el objeto '{normalizedPath}' en Google Drive.", normalizedPath);
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
            throw new InvalidOperationException("Storage:AccessToken es obligatorio para Google Drive.");
        }
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"Google Drive API respondió {(int)response.StatusCode}: {body}", null, response.StatusCode);
        }
    }

    private static async Task<string> ReadSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    private static string NormalizePath(string path, bool allowEmpty = false)
    {
        var value = (path ?? string.Empty).Replace('\\', '/').Trim('/');
        if (!allowEmpty && string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("La ruta no puede estar vacía.", nameof(path));
        }

        return value;
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
