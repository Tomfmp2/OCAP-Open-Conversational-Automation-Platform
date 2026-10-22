using System.Text.RegularExpressions;
using FluentValidation;

namespace OCAP.Api.Setup;

/// <summary>Primer paso de la configuración inicial: organización y cuenta de administrador.</summary>
public sealed record SetupRequest(
    string SetupCode,
    string OrganizationName,
    string AdminName,
    string AdminEmail,
    string AdminPassword);

public sealed record SetupStatusResponse(bool Required);

public sealed partial class SetupRequestValidator : AbstractValidator<SetupRequest>
{
    public const int MinPasswordLength = 12;

    public SetupRequestValidator()
    {
        RuleFor(x => x.SetupCode).NotEmpty().WithMessage("Ingresa el código de configuración que aparece en los logs de la API.");
        RuleFor(x => x.OrganizationName).NotEmpty().MaximumLength(100)
            .Must(name => !string.IsNullOrEmpty(ToSlug(name)))
            .WithMessage("El nombre de la organización debe tener al menos una letra o número.");
        RuleFor(x => x.AdminName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.AdminEmail).NotEmpty().EmailAddress().MaximumLength(254);
        RuleFor(x => x.AdminPassword).NotEmpty()
            .MinimumLength(MinPasswordLength)
            .WithMessage($"La contraseña debe tener al menos {MinPasswordLength} caracteres.")
            .Must(p => p.Any(char.IsLetter) && p.Any(char.IsDigit))
            .WithMessage("La contraseña debe combinar letras y números.");
    }

    /// <summary>"Acme Corp." → "acme-corp": el slug del tenant se deriva del nombre, el usuario no lo escribe.</summary>
    public static string ToSlug(string? name)
    {
        var ascii = new string((name ?? string.Empty)
            .Normalize(System.Text.NormalizationForm.FormD)
            .Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
            .ToArray())
            .ToLowerInvariant();
        return NonSlugChars().Replace(ascii, "-").Trim('-');
    }

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex NonSlugChars();
}
