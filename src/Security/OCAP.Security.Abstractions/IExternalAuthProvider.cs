using OCAP.Security.Abstractions.DTOs;

namespace OCAP.Security.Abstractions;

// Interfaz para un proveedor de autenticación externo individual (Google, Microsoft, GitHub, Generic OIDC) (CAP-15).
public interface IExternalAuthProvider
{
    // Nombre identificador único del proveedor (ej. "google", "microsoft", "github", "oidc").
    string ProviderName { get; }

    // Nombre amigable visible para el usuario (ej. "Google Workspace", "Microsoft Entra ID").
    string DisplayName { get; }

    // Indica si el proveedor está habilitado en la configuración.
    bool IsEnabled { get; }

    // Genera la URL de autorización hacia el proveedor externo.
    string BuildAuthorizationUrl(string redirectUri, string state);

    // Canjea el código de autorización por el payload de usuario verificado del proveedor externo.
    Task<ExternalUserPayloadDto?> ProcessCallbackAsync(string code, string redirectUri, CancellationToken cancellationToken = default);
}
