namespace OCAP.Knowledge.Domain.ValueObjects;

public record DocumentMetadata(
    string Author,
    string Category,
    string ContentHash,
    Dictionary<string, string> Properties
);
