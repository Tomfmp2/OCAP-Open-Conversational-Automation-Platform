namespace OCAP.Providers.Google.Abstractions;

/// <summary>
/// Token de acceso vigente para llamar a Google Workspace. La implementación decide la fuente
/// (conexión OAuth guardada o <c>Google:AccessToken</c> del entorno); los proveedores HTTP solo lo usan.
/// </summary>
public interface IGoogleAccessTokenProvider
{
    Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken);
}
