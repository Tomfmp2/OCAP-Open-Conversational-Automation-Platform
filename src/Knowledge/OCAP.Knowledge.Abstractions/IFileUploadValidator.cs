namespace OCAP.Knowledge.Abstractions;

public interface IFileUploadValidator
{
    ValidationResult ValidateFile(Stream stream, string fileName, string contentType, long maxFileSizeBytes = 26214400);
    string SanitizeFileName(string fileName);
    string ComputeSha256Hash(Stream stream);
}

public record ValidationResult(bool IsValid, string? ErrorMessage, string SanitizedFileName, string Extension);
