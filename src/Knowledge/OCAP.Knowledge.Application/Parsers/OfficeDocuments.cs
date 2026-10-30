using System.IO.Compression;
using System.Text;
using System.Xml.Linq;

namespace OCAP.Knowledge.Application.Parsers;

/// <summary>
/// Lectura de Word (.docx) y Excel (.xlsx): son ZIP con XML (Office Open XML), así que no hace falta una librería.
/// Se leen solo las partes de texto; macros, imágenes y objetos incrustados se ignoran.
/// </summary>
public static class OfficeDocuments
{
    private static readonly XNamespace W = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
    private static readonly XNamespace S = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static readonly XNamespace R = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private static readonly XNamespace PackageRels = "http://schemas.openxmlformats.org/package/2006/relationships";
    /// <summary>
    /// Tope de lo que se descomprime en total, contando los bytes que de verdad salen (el tamaño que declara el ZIP se
    /// puede falsear): un archivo pequeño puede expandirse a gigas (zip bomb).
    /// </summary>
    private const long MaxDecompressedBytes = 100 * 1024 * 1024;
    private const int MaxSheets = 50;
    /// <summary>Última columna de Excel (XFD): una referencia mayor es inventada.</summary>
    private const int MaxColumns = 16_384;
    /// <summary>Texto que se indexa como mucho de un documento; el resto se descarta.</summary>
    private const int MaxOutputChars = TabularText.DefaultMaxChars;
    private const int MaxRowsPerSheet = 100_000;

    /// <summary>Párrafos en orden; las tablas, fila por fila con sus encabezados.</summary>
    public static string ReadDocx(Stream stream)
    {
        using var zip = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
        var budget = new DecompressionBudget();
        var body = Load(zip, "word/document.xml", budget)?.Root?.Element(W + "body");
        if (body is null) return string.Empty;

        var output = new StringBuilder();
        foreach (var block in body.Elements())
        {
            if (output.Length > MaxOutputChars) break;
            if (block.Name == W + "p")
            {
                var text = ParagraphText(block);
                if (text.Length > 0) output.Append(text).Append('\n');
            }
            else if (block.Name == W + "tbl")
            {
                var rows = block.Elements(W + "tr")
                    .Select(tr => (IReadOnlyList<string>)tr.Elements(W + "tc").Select(tc => string.Join(" ", tc.Elements(W + "p").Select(ParagraphText))).ToList())
                    .ToList();
                output.Append('\n').Append(TabularText.Render(null, rows, Math.Max(0, MaxOutputChars - output.Length))).Append("\n\n");
            }
        }
        return output.ToString().Trim();
    }

    /// <summary>Cada hoja como tabla: «Hoja Precios. Producto: …; Precio: …».</summary>
    public static string ReadXlsx(Stream stream)
    {
        using var zip = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
        var budget = new DecompressionBudget();
        var shared = Load(zip, "xl/sharedStrings.xml", budget)?.Root?.Elements(S + "si")
            .Select(si => string.Concat(si.Descendants(S + "t").Select(t => t.Value)))
            .ToList() ?? [];

        var workbook = Load(zip, "xl/workbook.xml", budget);
        var rels = Load(zip, "xl/_rels/workbook.xml.rels", budget)?.Root?.Elements(PackageRels + "Relationship")
            .GroupBy(r => (string?)r.Attribute("Id") ?? string.Empty)
            .ToDictionary(g => g.Key, g => (string?)g.First().Attribute("Target") ?? string.Empty) ?? [];

        var output = new StringBuilder();
        var seen = new HashSet<string>();
        foreach (var sheet in (workbook?.Root?.Element(S + "sheets")?.Elements(S + "sheet") ?? []).Take(MaxSheets))
        {
            if (output.Length > MaxOutputChars) break;
            var name = (string?)sheet.Attribute("name") ?? "Hoja";
            if (!rels.TryGetValue((string?)sheet.Attribute(R + "id") ?? string.Empty, out var target)) continue;
            var path = target.StartsWith('/') ? target.TrimStart('/') : $"xl/{target}";
            if (!seen.Add(path)) continue; // varias hojas apuntando a la misma parte: se lee una vez
            var data = Load(zip, path, budget)?.Root?.Element(S + "sheetData");
            if (data is null) continue;

            var rows = data.Elements(S + "row").Take(MaxRowsPerSheet).Select(row => (IReadOnlyList<string>)Cells(row, shared)).ToList();
            var text = TabularText.Render($"Hoja {name}", rows, MaxOutputChars - output.Length);
            if (text.Length > 0) output.Append(text).Append("\n\n");
        }
        return output.ToString().Trim();
    }

    /// <summary>Celdas de una fila en su columna (las vacías no vienen en el XML y se rellenan).</summary>
    private static List<string> Cells(XElement row, IReadOnlyList<string> shared)
    {
        var cells = new List<string>();
        foreach (var c in row.Elements(S + "c").Take(MaxColumns))
        {
            var column = ColumnIndex((string?)c.Attribute("r"));
            // Solo se rellena hasta una columna real; una referencia inventada no puede reservar memoria sin límite.
            while (column is { } col && col < MaxColumns && cells.Count < col) cells.Add(string.Empty);
            var type = (string?)c.Attribute("t");
            var value = c.Element(S + "v")?.Value ?? string.Empty;
            cells.Add(type switch
            {
                "s" when int.TryParse(value, out var i) && i < shared.Count => shared[i],
                "inlineStr" => string.Concat(c.Descendants(S + "t").Select(t => t.Value)),
                "b" => value == "1" ? "Sí" : "No",
                _ => value
            });
        }
        return cells;
    }

    /// <summary>"C7" → 2 (columna C, base 0).</summary>
    private static int? ColumnIndex(string? reference)
    {
        if (string.IsNullOrEmpty(reference)) return null;
        var letters = reference.TakeWhile(char.IsAsciiLetter).ToArray();
        if (letters.Length is 0 or > 3) return null; // XFD es la última columna
        var index = 0;
        foreach (var ch in letters) index = index * 26 + (char.ToUpperInvariant(ch) - 'A' + 1);
        return index - 1 < MaxColumns ? index - 1 : null;
    }

    private static string ParagraphText(XElement paragraph)
    {
        var text = new StringBuilder();
        foreach (var node in paragraph.Descendants())
        {
            if (node.Name == W + "t") text.Append(node.Value);
            else if (node.Name == W + "tab") text.Append('\t');
            else if (node.Name == W + "br") text.Append('\n');
        }
        return text.ToString().Trim();
    }

    private static XDocument? Load(ZipArchive zip, string path, DecompressionBudget budget)
    {
        var entry = zip.GetEntry(path);
        if (entry is null) return null;
        using var part = new BudgetedStream(entry.Open(), budget);
        // Sin DTD: el XML de Office no las usa y así no hay entidades externas (XXE).
        return XDocument.Load(System.Xml.XmlReader.Create(part, new System.Xml.XmlReaderSettings { DtdProcessing = System.Xml.DtdProcessing.Prohibit }));
    }

    /// <summary>Bytes descomprimidos que quedan para todo el documento.</summary>
    private sealed class DecompressionBudget
    {
        public long Remaining { get; set; } = MaxDecompressedBytes;
    }

    /// <summary>Cuenta lo que realmente se lee de una parte y corta al agotar el presupuesto del documento.</summary>
    private sealed class BudgetedStream(Stream inner, DecompressionBudget budget) : Stream
    {
        public override int Read(byte[] buffer, int offset, int count)
        {
            var read = inner.Read(buffer, offset, count);
            budget.Remaining -= read;
            if (budget.Remaining < 0) throw new InvalidDataException("El documento es demasiado grande para leerlo.");
            return read;
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing) inner.Dispose();
            base.Dispose(disposing);
        }
    }
}
