using System.Security.Cryptography;
using System.Text.RegularExpressions;
using OCAP.Knowledge.Abstractions;

namespace OCAP.Knowledge.Application.Services;

// Validador de seguridad para carga de archivos empresarial. Protege contra vulnerabilidades Path Traversal, archivos corruptos y ejecución remota.
public class FileUploadValidator : IFileUploadValidator
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".docx", ".txt", ".md", ".markdown", ".csv", ".json", ".html", ".htm", ".xml"
    };

    private static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "text/plain",
        "text/markdown",
        "text/csv",
        "application/json",
        "text/html",
        "text/xml",
        "application/xml",
        "application/octet-stream"
    };

    public ValidationResult ValidateFile(Stream stream, string fileName, string contentType, long maxFileSizeBytes = 26214400)
    {
        // 1. Validar que el stream exista y no esté vacío
        if (stream == null || stream.Length == 0)
        {
            return new ValidationResult(false, "El archivo proporcionado está vacío o es nulo.", string.Empty, string.Empty);
        }

        // 2. Validar tamaño máximo permitido
        if (stream.Length > maxFileSizeBytes)
        {
            double maxMb = maxFileSizeBytes / (1024.0 * 1024.0);
            return new ValidationResult(false, $"El archivo excede el tamaño máximo permitido de {maxMb:F1} MB.", string.Empty, string.Empty);
        }

        // 3. Sanitización de nombre de archivo y prevención de Path Traversal
        var sanitizedName = SanitizeFileName(fileName);
        if (string.IsNullOrWhiteSpace(sanitizedName))
        {
            return new ValidationResult(false, "El nombre del archivo no es válido después de la sanitización.", string.Empty, string.Empty);
        }

        // 4. Validar extensión permitida
        var extension = Path.GetExtension(sanitizedName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
        {
            return new ValidationResult(false, $"La extensión '{extension}' no está permitida por políticas de seguridad.", sanitizedName, extension);
        }

        // 5. Validar tipo MIME si se proporciona
        if (!string.IsNullOrWhiteSpace(contentType) && !AllowedMimeTypes.Contains(contentType.Split(';')[0].Trim()))
        {
            return new ValidationResult(false, $"El tipo MIME '{contentType}' no está permitido.", sanitizedName, extension);
        }

        return new ValidationResult(true, null, sanitizedName, extension);
    }

    public string SanitizeFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return string.Empty;

        // Remover rutas absolutas o relativas (Prevención de Path Traversal)
        var name = Path.GetFileName(fileName);

        // Remover caracteres inválidos del sistema de archivos
        var invalidChars = Path.GetInvalidFileNameChars();
        var cleanName = new string(name.Where(ch => !invalidChars.Contains(ch)).ToArray());

        // Reemplazar secuencias peligrosas
        cleanName = Regex.Replace(cleanName, @"(\.\.[\/\\])+", string.Empty);
        cleanName = cleanName.Trim('.', ' ');

        return cleanName;
    }

    public string ComputeSha256Hash(Stream stream)
    {
        if (stream == null || !stream.CanRead) return string.Empty;

        long originalPosition = 0;
        if (stream.CanSeek)
        {
            originalPosition = stream.Position;
            stream.Position = 0;
        }

        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(stream);

        if (stream.CanSeek)
        {
            stream.Position = originalPosition;
        }

        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
