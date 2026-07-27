using OCAP.Knowledge.Domain.Enums;

namespace OCAP.Knowledge.Abstractions;

public interface IDocumentParserFactory
{
    IDocumentParser GetParser(DocumentType documentType);
}
