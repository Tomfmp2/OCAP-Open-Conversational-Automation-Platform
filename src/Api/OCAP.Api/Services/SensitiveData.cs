using System.Text.RegularExpressions;

namespace OCAP.Api.Services;

/// <summary>Oculta números de tarjeta y secretos que un cliente escribe en un chat, antes de guardarlos.</summary>
public static partial class SensitiveData
{
    private const string HiddenCard = "[tarjeta oculta]";
    private const string HiddenSecret = "[oculto]";

    public static string Mask(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        var withoutCards = CardNumber().Replace(text, m => IsCardNumber(m.Value) ? HiddenCard : m.Value);
        return Secret().Replace(withoutCards, m =>
            IsSecretValue(m.Groups["word"].Value, m.Groups["sep"].Value, m.Groups["value"].Value)
                ? $"{m.Groups["word"].Value}{m.Groups["sep"].Value}{HiddenSecret}"
                : m.Value);
    }

    /// <summary>Luhn: un número de pedido o de teléfono largo no es una tarjeta y debe seguir visible.</summary>
    private static bool IsCardNumber(string candidate)
    {
        var digits = candidate.Where(char.IsDigit).Select(c => c - '0').ToArray();
        if (digits.Length is < 13 or > 19) return false;
        var sum = 0;
        for (var i = 0; i < digits.Length; i++)
        {
            var digit = digits[digits.Length - 1 - i];
            if (i % 2 == 1)
            {
                digit *= 2;
                if (digit > 9) digit -= 9;
            }
            sum += digit;
        }
        return sum % 10 == 0;
    }

    /// <summary>«La clave es la paciencia» no es un secreto: tras «es», «clave» solo cuenta si el valor lleva números o mayúsculas.</summary>
    private static bool IsSecretValue(string word, string separator, string value) =>
        !word.Equals("clave", StringComparison.OrdinalIgnoreCase)
        || separator.Contains(':') || separator.Contains('=')
        || value.TrimEnd('.', ',', ';').Any(c => !char.IsLower(c));

    [GeneratedRegex(@"\b(?:\d[ -]?){12,18}\d\b")]
    private static partial Regex CardNumber();

    [GeneratedRegex(@"\b(?<word>contrase(?:ñ|n)a|password|clave|pin|cvv|cvc)\b(?<sep>\s+es\s+|\s*[:=]\s*)(?<value>\S+)", RegexOptions.IgnoreCase)]
    private static partial Regex Secret();
}
