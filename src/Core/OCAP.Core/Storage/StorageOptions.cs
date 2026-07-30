namespace OCAP.Core.Storage;

/// <summary>
/// Configuración común para los adaptadores de almacenamiento de objetos.
/// Se enlaza desde la sección <c>Storage</c>.
/// </summary>
public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    public string Provider { get; set; } = "Local";
    public string RootPath { get; set; } = "./storage";

    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string Region { get; set; } = "us-east-1";
    public string Bucket { get; set; } = string.Empty;
    public string? ServiceUrl { get; set; }

    public string AccessToken { get; set; } = string.Empty;

    public string ConnectionString { get; set; } = string.Empty;
    public string Container { get; set; } = string.Empty;
}
