namespace OCAP.Api.Security;

/// <summary>Regla única de contraseñas: configuración inicial, invitaciones, recuperación y cambio de contraseña.</summary>
public static class PasswordRules
{
    public const int MinLength = 12;

    /// <summary>Motivo del rechazo o null si la contraseña es válida.</summary>
    public static string? Validate(string? password)
    {
        if (string.IsNullOrEmpty(password) || password.Length < MinLength)
            return $"La contraseña debe tener al menos {MinLength} caracteres.";
        if (password.Length > 256) return "La contraseña es demasiado larga.";
        if (!password.Any(char.IsLetter) || !password.Any(char.IsDigit))
            return "La contraseña debe combinar letras y números.";
        return null;
    }
}
