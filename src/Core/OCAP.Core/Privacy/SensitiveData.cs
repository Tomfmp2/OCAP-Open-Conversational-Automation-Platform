using System.Text;
using System.Text.RegularExpressions;

namespace OCAP.Core.Privacy;

/// <summary>Oculta números de tarjeta y secretos que una persona escribe en un chat, antes de guardarlos o publicarlos.</summary>
public static partial class SensitiveData
{
    private const string HiddenCard = "[tarjeta oculta]";
    private const string HiddenSecret = "[oculto]";

    private const int MinCardDigits = 13;
    private const int MaxCardDigits = 19;

    public static string Mask(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        // NFC: «contraseña» puede llegar con la tilde como carácter aparte y no coincidiría con el patrón.
        var masked = DigitRun().Replace(text.Normalize(NormalizationForm.FormC), m => ContainsCard(m.Value) ? HiddenCard : m.Value);
        masked = LabeledSecret().Replace(masked, m => $"{m.Groups["word"].Value}{m.Groups["sep"].Value}{HiddenSecret}");
        masked = NumericCode().Replace(masked, m => $"{m.Groups["word"].Value}{m.Groups["sep"].Value}{HiddenSecret}");
        return StatedSecret().Replace(masked, m =>
            IsSecretValue(m.Groups["word"].Value, m.Groups["value"].Value)
                ? $"{m.Groups["word"].Value}{m.Groups["sep"].Value}{HiddenSecret}"
                : m.Value);
    }

    /// <summary>
    /// Busca la tarjeta en cualquier tramo del número: puede venir pegada a un CVV o a otra cifra. Si la encuentra se
    /// oculta todo el tramo, porque lo que la acompaña suele ser su CVV o su vencimiento.
    /// </summary>
    private static bool ContainsCard(string run)
    {
        var digits = run.Where(char.IsDigit).Select(c => c - '0').ToArray();
        if (digits.Length is >= MinCardDigits and <= MaxCardDigits && PassesLuhn(digits)) return true;

        // Dentro de un número más largo solo se buscan los formatos habituales (Visa, Mastercard y Amex): con todos los
        // largos, casi cualquier número de pedido contendría por azar un tramo válido.
        for (var start = 0; start < digits.Length; start++)
        {
            var length = digits[start] switch { 3 => 15, 2 or 4 or 5 or 6 => 16, _ => 0 };
            if (length > 0 && start + length <= digits.Length && PassesLuhn(digits.AsSpan(start, length))) return true;
        }
        return false;
    }

    private static bool PassesLuhn(ReadOnlySpan<int> digits)
    {
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

    /// <summary>
    /// «La clave es la paciencia» y «el pin de la camisa es grande» no son secretos: «clave» solo cuenta si el valor lleva
    /// números o mayúsculas, y un código solo si lleva números.
    /// </summary>
    private static bool IsSecretValue(string word, string value)
    {
        var trimmed = value.TrimEnd('.', ',', ';');
        if (word.Equals("clave", StringComparison.OrdinalIgnoreCase)) return trimmed.Any(c => !char.IsLower(c));
        if (word.Equals("contraseña", StringComparison.OrdinalIgnoreCase)
            || word.Equals("contrasena", StringComparison.OrdinalIgnoreCase)
            || word.Equals("password", StringComparison.OrdinalIgnoreCase)) return true;
        return trimmed.Any(char.IsDigit);
    }

    [GeneratedRegex(@"\d(?:[ .\-]{0,3}\d){12,}")]
    private static partial Regex DigitRun();

    /// <summary>«contraseña: dos palabras»: con dos puntos o igual, el valor llega hasta el final de la línea.</summary>
    [GeneratedRegex(@"\b(?<word>contraseña|contrasena|password|clave)\b(?<sep>\s*[:=]\s*)(?<value>[^\r\n]+)", RegexOptions.IgnoreCase)]
    private static partial Regex LabeledSecret();

    /// <summary>«mi pin 4821», «cvv de 123»: un código numérico no necesita más contexto.</summary>
    [GeneratedRegex(@"\b(?<word>pin|nip|cvv|cvc)\b(?<sep>(?:\s+(?:es|is|de|sería|seria))?(?:\s*[:=]\s*|\s+))(?<value>\d{3,8})\b", RegexOptions.IgnoreCase)]
    private static partial Regex NumericCode();

    /// <summary>«mi clave es X», «la contraseña de mi correo es X», «my password is X».</summary>
    [GeneratedRegex(@"\b(?<word>contraseña|contrasena|password|clave|pin|nip|cvv|cvc)\b(?<sep>(?:\s+de\s+[^\r\n.,;:]{1,30}?)?\s+(?:es|is|sería|seria)\s+)(?<value>\S+)", RegexOptions.IgnoreCase)]
    private static partial Regex StatedSecret();
}
