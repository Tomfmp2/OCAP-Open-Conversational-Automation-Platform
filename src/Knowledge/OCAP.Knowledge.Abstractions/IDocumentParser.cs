using OCAP.Knowledge.Domain.Enums;

namespace OCAP.Knowledge.Abstractions;

public interface IDocumentParser
{
    DocumentType SupportedType { get; }
    Task<ParsedDocumentResult> ParseAsync(Stream stream, string fileName, CancellationToken cancellationToken = default);
}
