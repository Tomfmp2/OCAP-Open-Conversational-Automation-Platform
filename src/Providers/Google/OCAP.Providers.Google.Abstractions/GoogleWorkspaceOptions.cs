namespace OCAP.Providers.Google.Abstractions;

/// <summary>
/// Configuración compartida por los proveedores HTTP de Google Workspace.
/// </summary>
public sealed class GoogleWorkspaceOptions
{
    public const string SectionName = "Google";

    public string AccessToken { get; set; } = string.Empty;
    public bool UseInMemory { get; set; }
}
