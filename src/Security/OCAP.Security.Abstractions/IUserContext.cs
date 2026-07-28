namespace OCAP.Security.Abstractions;

/// <summary>
/// Proporciona acceso al contexto del usuario autenticado en la petición actual.
/// </summary>
public interface IUserContext
{
    /// <summary>
    /// Identificador único del usuario.
    /// </summary>
    Guid UserId { get; }

    /// <summary>
    /// Nombre de usuario o Login.
    /// </summary>
    string UserName { get; }

    /// <summary>
    /// Correo electrónico del usuario.
    /// </summary>
    string Email { get; }

    /// <summary>
    /// Indica si la petición actual proviene de un usuario autenticado.
    /// </summary>
    bool IsAuthenticated { get; }
}
