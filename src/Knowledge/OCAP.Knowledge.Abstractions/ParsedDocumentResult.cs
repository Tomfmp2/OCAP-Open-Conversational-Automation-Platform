using OCAP.Knowledge.Domain.Enums;

namespace OCAP.Knowledge.Abstractions;

public record ParsedDocumentResult(
    string Text,
    List<string> Tables,
    string MetadataJson,
    string Author,
    DateTime Date,
    DocumentCategory Category,
    string Version,
    string ContentHash
);
