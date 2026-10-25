using OCAP.Knowledge.Domain.Enums;

namespace OCAP.Api.Validation;

/// <summary>
/// Valida un documento subido a la base de conocimiento por su contenido, no solo por la extensión: un ejecutable
/// renombrado a .pdf o un binario con extensión de texto se rechazan antes de procesarse.
/// </summary>
public static class KnowledgeUploadValidator
{
    public const long MaxBytes = 10 * 1024 * 1024;
    private const int SniffBytes = 8 * 1024;

    private static readonly Dictionary<string, DocumentType> Types = new(StringComparer.OrdinalIgnoreCase)
    {
        ["pdf"] = DocumentType.Pdf,
        ["docx"] = DocumentType.Docx,
        ["md"] = DocumentType.Markdown,
        ["markdown"] = DocumentType.Markdown,
        ["csv"] = DocumentType.Csv,
        ["json"] = DocumentType.Json,
        ["html"] = DocumentType.Html,
        ["htm"] = DocumentType.Html,
        ["xml"] = DocumentType.Xml,
        ["txt"] = DocumentType.Txt
    };

    public static string AllowedExtensions => string.Join(", ", Types.Keys.Select(k => "." + k));

    /// <summary>Tipo del documento, nombre seguro para mostrar y el motivo del rechazo (null si es válido).</summary>
    public static async Task<(DocumentType Type, string SafeName, string? Error)> ValidateAsync(IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length == 0) return (default, string.Empty, "El archivo está vacío.");
        if (file.Length > MaxBytes) return (default, string.Empty, "El archivo supera los 10 MB.");

        var safeName = SafeFileName(file.FileName);
        var extension = Path.GetExtension(safeName).TrimStart('.');
        if (!Types.TryGetValue(extension, out var type))
            return (default, safeName, $"Tipo de archivo no admitido. Formatos permitidos: {AllowedExtensions}.");

        var head = new byte[SniffBytes];
        int read;
        await using (var stream = file.OpenReadStream())
        {
            read = await stream.ReadAtLeastAsync(head, SniffBytes, throwOnEndOfStream: false, cancellationToken);
        }
        var sample = head.AsSpan(0, read);

        var matches = type switch
        {
            DocumentType.Pdf => sample.StartsWith("%PDF-"u8),
            DocumentType.Docx => sample.StartsWith("PK\x03\x04"u8),
            // Texto: sin bytes nulos (los binarios casi siempre los tienen) y UTF-8 válido.
            _ => !sample.Contains((byte)0) && IsUtf8(sample, read == SniffBytes)
        };

        return matches
            ? (type, safeName, null)
            : (type, safeName, $"El contenido no corresponde a un archivo .{extension}.");
    }

    private static bool IsUtf8(ReadOnlySpan<byte> sample, bool truncated)
    {
        // La muestra puede cortar un carácter multibyte al final: se toleran hasta 3 bytes incompletos.
        for (var trim = 0; trim <= (truncated ? 3 : 0); trim++)
        {
            if (System.Text.Unicode.Utf8.IsValid(sample[..^trim])) return true;
        }
        return false;
    }

    private static string SafeFileName(string fileName)
    {
        var name = Path.GetFileName(fileName.Replace('\\', '/'));
        var clean = new string(name.Where(c => !char.IsControl(c)).ToArray()).Trim();
        return string.IsNullOrEmpty(clean) ? "documento" : clean[..Math.Min(clean.Length, 200)];
    }
}
