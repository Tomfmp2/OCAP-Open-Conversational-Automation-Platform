using OCAP.Agents.Abstractions.Providers;
using OCAP.Knowledge.Abstractions;

namespace OCAP.Api.Validation;

/// <summary>
/// Adjuntos de un mensaje al asistente. El tipo se comprueba por el contenido, no por la extensión. Imágenes y video
/// van al modelo tal cual; de los documentos se extrae el texto (sirve con cualquier modelo y cuesta menos).
/// </summary>
public static class AssistantAttachments
{
    public const int MaxFiles = 4;
    public const long MaxImageBytes = 5 * 1024 * 1024;
    // El video cuesta muchos tokens y Gemini lo admite en línea hasta ~20 MB por petición.
    public const long MaxVideoBytes = 15 * 1024 * 1024;
    public const long MaxRequestBytes = 32 * 1024 * 1024;
    private const int MaxDocumentChars = 30_000;

    public sealed record Prepared(IReadOnlyList<PromptAttachment> Media, string DocumentText, IReadOnlyList<string> FileNames, string? Error);

    public static async Task<Prepared> PrepareAsync(IReadOnlyList<IFormFile>? files, IDocumentParserFactory parsers, CancellationToken cancellationToken)
    {
        if (files is null || files.Count == 0) return Fail("Adjunta al menos un archivo.");
        if (files.Count > MaxFiles) return Fail($"Puedes adjuntar hasta {MaxFiles} archivos por mensaje.");

        var media = new List<PromptAttachment>();
        var documents = new System.Text.StringBuilder();
        var names = new List<string>();
        foreach (var file in files)
        {
            var name = Path.GetFileName(file.FileName);
            names.Add(name);
            if (file.Length == 0) return Fail($"«{name}» está vacío.");

            var bytes = new byte[file.Length];
            await using (var stream = file.OpenReadStream())
            {
                await stream.ReadExactlyAsync(bytes, cancellationToken);
            }

            if (DetectMedia(bytes) is { } mime)
            {
                var isVideo = mime.StartsWith("video/", StringComparison.Ordinal);
                if (isVideo && media.Any(m => m.MimeType.StartsWith("video/", StringComparison.Ordinal)))
                    return Fail("Solo un video por mensaje.");
                if (file.Length > (isVideo ? MaxVideoBytes : MaxImageBytes))
                    return Fail($"«{name}» supera los {(isVideo ? MaxVideoBytes : MaxImageBytes) / (1024 * 1024)} MB.");
                media.Add(new PromptAttachment(mime, bytes));
                continue;
            }

            var (type, safeName, error) = await KnowledgeUploadValidator.ValidateAsync(file, cancellationToken);
            if (error is not null) return Fail($"«{name}»: {error} También se admiten imágenes (PNG, JPG, WebP, GIF) y video (MP4, WebM, MOV).");
            using var content = new MemoryStream(bytes, writable: false);
            var parsed = await parsers.GetParser(type).ParseAsync(content, safeName, cancellationToken);
            documents.Append("Contenido de «").Append(safeName).Append("»:\n").Append(parsed.Text.Trim()).Append("\n\n");
        }

        var text = documents.ToString().Trim();
        if (text.Length > MaxDocumentChars) text = text[..MaxDocumentChars] + "\n[Documento recortado: es demasiado largo para un mensaje.]";
        return new Prepared(media, text, names, null);
    }

    private static Prepared Fail(string error) => new([], string.Empty, [], error);

    private static string? DetectMedia(ReadOnlySpan<byte> data)
    {
        if (data.StartsWith((ReadOnlySpan<byte>)[0x89, 0x50, 0x4E, 0x47])) return "image/png";
        if (data.StartsWith((ReadOnlySpan<byte>)[0xFF, 0xD8, 0xFF])) return "image/jpeg";
        if (data.StartsWith("GIF8"u8)) return "image/gif";
        if (data.Length > 12 && data.StartsWith("RIFF"u8) && data[8..12].SequenceEqual("WEBP"u8)) return "image/webp";
        if (data.StartsWith((ReadOnlySpan<byte>)[0x1A, 0x45, 0xDF, 0xA3])) return "video/webm";
        if (data.Length > 12 && data[4..8].SequenceEqual("ftyp"u8))
            return data[8..12].SequenceEqual("qt  "u8) ? "video/quicktime" : "video/mp4";
        return null;
    }
}
