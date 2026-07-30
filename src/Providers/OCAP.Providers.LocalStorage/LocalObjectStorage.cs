using OCAP.Core.Storage;

namespace OCAP.Providers.LocalStorage;

public sealed class LocalObjectStorage : IObjectStorage
{
    private readonly string _rootPath;
    private readonly string _rootPrefix;

    public LocalObjectStorage(StorageOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _rootPath = Path.GetFullPath(string.IsNullOrWhiteSpace(options.RootPath) ? "./storage" : options.RootPath);
        _rootPrefix = _rootPath.EndsWith(Path.DirectorySeparatorChar)
            ? _rootPath
            : _rootPath + Path.DirectorySeparatorChar;
        Directory.CreateDirectory(_rootPath);
    }

    public string ProviderName => "Local";

    public async Task UploadAsync(string path, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        var fullPath = ResolvePath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await using var destination = new FileStream(
            fullPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous);
        await content.CopyToAsync(destination, cancellationToken);
    }

    public Task<Stream> DownloadAsync(string path, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Stream stream = new FileStream(
            ResolvePath(path), FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string path, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        File.Delete(ResolvePath(path));
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(File.Exists(ResolvePath(path)));
    }

    public Task<IReadOnlyList<string>> ListAsync(string prefix, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var normalizedPrefix = NormalizeRelativePath(prefix, allowEmpty: true);
        if (!Directory.Exists(_rootPath))
        {
            return Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
        }

        IReadOnlyList<string> results = Directory
            .EnumerateFiles(_rootPath, "*", SearchOption.AllDirectories)
            .Select(file => Path.GetRelativePath(_rootPath, file).Replace(Path.DirectorySeparatorChar, '/'))
            .Where(file => file.StartsWith(normalizedPrefix, StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)
            .ToArray();
        return Task.FromResult(results);
    }

    public Task<bool> HealthAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            Directory.CreateDirectory(_rootPath);
            return Task.FromResult(Directory.Exists(_rootPath));
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    private string ResolvePath(string path)
    {
        var relative = NormalizeRelativePath(path, allowEmpty: false);
        var fullPath = Path.GetFullPath(Path.Combine(_rootPath, relative));
        if (!fullPath.StartsWith(_rootPrefix, StringComparison.Ordinal))
        {
            throw new ArgumentException("La ruta debe permanecer dentro de la raíz configurada.", nameof(path));
        }

        return fullPath;
    }

    private static string NormalizeRelativePath(string path, bool allowEmpty)
    {
        var value = (path ?? string.Empty).Replace('\\', '/').TrimStart('/');
        if (!allowEmpty && string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("La ruta no puede estar vacía.", nameof(path));
        }

        return value;
    }
}
