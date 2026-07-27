using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using OCAP.Knowledge.Abstractions;
using OCAP.Knowledge.Domain.Enums;

namespace OCAP.Knowledge.Application.Parsers;

public abstract class BaseDocumentParser : IDocumentParser
{
    public abstract DocumentType SupportedType { get; }

    public async Task<ParsedDocumentResult> ParseAsync(Stream stream, string fileName, CancellationToken cancellationToken = default)
    {
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream, cancellationToken);
        memoryStream.Position = 0;

        var rawBytes = memoryStream.ToArray();
        var contentHash = ComputeSha256(rawBytes);

        return await ParseContentAsync(memoryStream, fileName, contentHash, cancellationToken);
    }

    protected abstract Task<ParsedDocumentResult> ParseContentAsync(MemoryStream stream, string fileName, string contentHash, CancellationToken cancellationToken);

    protected static string ComputeSha256(byte[] data)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(data);
        return Convert.ToHexStringLower(hash);
    }
}

public class PdfDocumentParser : BaseDocumentParser
{
    public override DocumentType SupportedType => DocumentType.Pdf;

    protected override Task<ParsedDocumentResult> ParseContentAsync(MemoryStream stream, string fileName, string contentHash, CancellationToken cancellationToken)
    {
        var rawText = Encoding.UTF8.GetString(stream.ToArray());
        // Clean non-printable PDF stream bytes for plain text fallback
        var text = Regex.Replace(rawText, @"[^\x20-\x7E\x0A\x0D\x09]", " ");
        var tables = new List<string>();

        var result = new ParsedDocumentResult(
            Text: string.IsNullOrWhiteSpace(text) ? $"[PDF Document: {fileName}]" : text,
            Tables: tables,
            MetadataJson: JsonSerializer.Serialize(new { PageCount = 1, Format = "PDF" }),
            Author: "PDF Document Author",
            Date: DateTime.UtcNow,
            Category: DocumentCategory.General,
            Version: "1.0.0",
            ContentHash: contentHash
        );

        return Task.FromResult(result);
    }
}

public class DocxDocumentParser : BaseDocumentParser
{
    public override DocumentType SupportedType => DocumentType.Docx;

    protected override Task<ParsedDocumentResult> ParseContentAsync(MemoryStream stream, string fileName, string contentHash, CancellationToken cancellationToken)
    {
        var text = Encoding.UTF8.GetString(stream.ToArray());
        var cleanText = Regex.Replace(text, @"<[^>]+>", " "); // Strip XML tags fallback

        var result = new ParsedDocumentResult(
            Text: string.IsNullOrWhiteSpace(cleanText) ? $"[DOCX Document: {fileName}]" : cleanText,
            Tables: new List<string>(),
            MetadataJson: JsonSerializer.Serialize(new { Format = "DOCX" }),
            Author: "DOCX Author",
            Date: DateTime.UtcNow,
            Category: DocumentCategory.Technical,
            Version: "1.0.0",
            ContentHash: contentHash
        );

        return Task.FromResult(result);
    }
}

public class TxtDocumentParser : BaseDocumentParser
{
    public override DocumentType SupportedType => DocumentType.Txt;

    protected override Task<ParsedDocumentResult> ParseContentAsync(MemoryStream stream, string fileName, string contentHash, CancellationToken cancellationToken)
    {
        var text = Encoding.UTF8.GetString(stream.ToArray());

        var result = new ParsedDocumentResult(
            Text: text,
            Tables: new List<string>(),
            MetadataJson: JsonSerializer.Serialize(new { Encoding = "UTF-8", Length = text.Length }),
            Author: "System",
            Date: DateTime.UtcNow,
            Category: DocumentCategory.General,
            Version: "1.0.0",
            ContentHash: contentHash
        );

        return Task.FromResult(result);
    }
}

public class MarkdownDocumentParser : BaseDocumentParser
{
    public override DocumentType SupportedType => DocumentType.Markdown;

    protected override Task<ParsedDocumentResult> ParseContentAsync(MemoryStream stream, string fileName, string contentHash, CancellationToken cancellationToken)
    {
        var mdText = Encoding.UTF8.GetString(stream.ToArray());
        
        // Extract Markdown tables
        var tables = new List<string>();
        var tableMatches = Regex.Matches(mdText, @"\|.+\|(?:\r?\n\|.+----+.+\|)+(?:\r?\n\|.+\|)+");
        foreach (Match match in tableMatches)
        {
            tables.Add(match.Value);
        }

        var result = new ParsedDocumentResult(
            Text: mdText,
            Tables: tables,
            MetadataJson: JsonSerializer.Serialize(new { Format = "Markdown", TableCount = tables.Count }),
            Author: "Markdown Author",
            Date: DateTime.UtcNow,
            Category: DocumentCategory.Technical,
            Version: "1.0.0",
            ContentHash: contentHash
        );

        return Task.FromResult(result);
    }
}

public class CsvDocumentParser : BaseDocumentParser
{
    public override DocumentType SupportedType => DocumentType.Csv;

    protected override Task<ParsedDocumentResult> ParseContentAsync(MemoryStream stream, string fileName, string contentHash, CancellationToken cancellationToken)
    {
        var csvText = Encoding.UTF8.GetString(stream.ToArray());
        var lines = csvText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

        var result = new ParsedDocumentResult(
            Text: csvText,
            Tables: new List<string> { csvText },
            MetadataJson: JsonSerializer.Serialize(new { RowCount = lines.Length }),
            Author: "CSV Exporter",
            Date: DateTime.UtcNow,
            Category: DocumentCategory.Financial,
            Version: "1.0.0",
            ContentHash: contentHash
        );

        return Task.FromResult(result);
    }
}

public class JsonDocumentParser : BaseDocumentParser
{
    public override DocumentType SupportedType => DocumentType.Json;

    protected override Task<ParsedDocumentResult> ParseContentAsync(MemoryStream stream, string fileName, string contentHash, CancellationToken cancellationToken)
    {
        var jsonText = Encoding.UTF8.GetString(stream.ToArray());

        var result = new ParsedDocumentResult(
            Text: jsonText,
            Tables: new List<string>(),
            MetadataJson: JsonSerializer.Serialize(new { Format = "JSON" }),
            Author: "API Generator",
            Date: DateTime.UtcNow,
            Category: DocumentCategory.Technical,
            Version: "1.0.0",
            ContentHash: contentHash
        );

        return Task.FromResult(result);
    }
}

public class HtmlDocumentParser : BaseDocumentParser
{
    public override DocumentType SupportedType => DocumentType.Html;

    protected override Task<ParsedDocumentResult> ParseContentAsync(MemoryStream stream, string fileName, string contentHash, CancellationToken cancellationToken)
    {
        var html = Encoding.UTF8.GetString(stream.ToArray());
        var cleanText = Regex.Replace(html, @"<script[^>]*>[\s\S]*?</script>", "", RegexOptions.IgnoreCase);
        cleanText = Regex.Replace(cleanText, @"<style[^>]*>[\s\S]*?</style>", "", RegexOptions.IgnoreCase);
        cleanText = Regex.Replace(cleanText, @"<[^>]+>", " ");
        cleanText = Regex.Replace(cleanText, @"\s+", " ").Trim();

        var result = new ParsedDocumentResult(
            Text: cleanText,
            Tables: new List<string>(),
            MetadataJson: JsonSerializer.Serialize(new { Format = "HTML" }),
            Author: "Web Content",
            Date: DateTime.UtcNow,
            Category: DocumentCategory.General,
            Version: "1.0.0",
            ContentHash: contentHash
        );

        return Task.FromResult(result);
    }
}

public class XmlDocumentParser : BaseDocumentParser
{
    public override DocumentType SupportedType => DocumentType.Xml;

    protected override Task<ParsedDocumentResult> ParseContentAsync(MemoryStream stream, string fileName, string contentHash, CancellationToken cancellationToken)
    {
        var xml = Encoding.UTF8.GetString(stream.ToArray());
        var cleanText = Regex.Replace(xml, @"<[^>]+>", " ");
        cleanText = Regex.Replace(cleanText, @"\s+", " ").Trim();

        var result = new ParsedDocumentResult(
            Text: cleanText,
            Tables: new List<string>(),
            MetadataJson: JsonSerializer.Serialize(new { Format = "XML" }),
            Author: "XML Export",
            Date: DateTime.UtcNow,
            Category: DocumentCategory.Technical,
            Version: "1.0.0",
            ContentHash: contentHash
        );

        return Task.FromResult(result);
    }
}

public class DocumentParserFactory : IDocumentParserFactory
{
    private readonly IEnumerable<IDocumentParser> _parsers;

    public DocumentParserFactory(IEnumerable<IDocumentParser> parsers)
    {
        _parsers = parsers ?? throw new ArgumentNullException(nameof(parsers));
    }

    public IDocumentParser GetParser(DocumentType documentType)
    {
        var parser = _parsers.FirstOrDefault(p => p.SupportedType == documentType);
        return parser ?? new TxtDocumentParser();
    }
}
