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
    /// <summary>Tope de lo que se descomprime: un ZIP pequeño puede expandirse a gigas (zip bomb).</summary>
    private const long MaxPartBytes = 50 * 1024 * 1024;

    /// <summary>Párrafos en orden; las tablas, fila por fila con sus encabezados.</summary>
    public static string ReadDocx(Stream stream)
    {
        using var zip = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
        var body = Load(zip, "word/document.xml")?.Root?.Element(W + "body");
        if (body is null) return string.Empty;

        var output = new StringBuilder();
        foreach (var block in body.Elements())
        {
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
                output.Append('\n').Append(TabularText.Render(null, rows)).Append("\n\n");
            }
        }
        return output.ToString().Trim();
    }

    /// <summary>Cada hoja como tabla: «Hoja Precios. Producto: …; Precio: …».</summary>
    public static string ReadXlsx(Stream stream)
    {
        using var zip = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
        var shared = Load(zip, "xl/sharedStrings.xml")?.Root?.Elements(S + "si")
            .Select(si => string.Concat(si.Descendants(S + "t").Select(t => t.Value)))
            .ToList() ?? [];

        var workbook = Load(zip, "xl/workbook.xml");
        var rels = Load(zip, "xl/_rels/workbook.xml.rels")?.Root?.Elements(PackageRels + "Relationship")
            .ToDictionary(r => (string)r.Attribute("Id")!, r => (string)r.Attribute("Target")!) ?? [];

        var output = new StringBuilder();
        foreach (var sheet in workbook?.Root?.Element(S + "sheets")?.Elements(S + "sheet") ?? [])
        {
            var name = (string?)sheet.Attribute("name") ?? "Hoja";
            if (!rels.TryGetValue((string?)sheet.Attribute(R + "id") ?? string.Empty, out var target)) continue;
            var path = target.StartsWith('/') ? target.TrimStart('/') : $"xl/{target}";
            var data = Load(zip, path)?.Root?.Element(S + "sheetData");
            if (data is null) continue;

            var rows = data.Elements(S + "row").Select(row => (IReadOnlyList<string>)Cells(row, shared)).ToList();
            var text = TabularText.Render($"Hoja {name}", rows);
            if (text.Length > 0) output.Append(text).Append("\n\n");
        }
        return output.ToString().Trim();
    }

    /// <summary>Celdas de una fila en su columna (las vacías no vienen en el XML y se rellenan).</summary>
    private static List<string> Cells(XElement row, IReadOnlyList<string> shared)
    {
        var cells = new List<string>();
        foreach (var c in row.Elements(S + "c"))
        {
            var column = ColumnIndex((string?)c.Attribute("r"));
            while (column is { } col && cells.Count < col) cells.Add(string.Empty);
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
        var index = 0;
        foreach (var ch in reference.TakeWhile(char.IsLetter)) index = index * 26 + (char.ToUpperInvariant(ch) - 'A' + 1);
        return index == 0 ? null : index - 1;
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

    private static XDocument? Load(ZipArchive zip, string path)
    {
        var entry = zip.GetEntry(path);
        if (entry is null) return null;
        if (entry.Length > MaxPartBytes) throw new InvalidDataException("El documento es demasiado grande para leerlo.");
        using var part = entry.Open();
        // Sin DTD: el XML de Office no las usa y así no hay entidades externas (XXE).
        return XDocument.Load(System.Xml.XmlReader.Create(part, new System.Xml.XmlReaderSettings { DtdProcessing = System.Xml.DtdProcessing.Prohibit }));
    }
}
