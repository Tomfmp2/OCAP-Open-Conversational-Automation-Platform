using System.IO.Compression;
using System.Text;
using OCAP.Knowledge.Application.Chunkers;
using OCAP.Knowledge.Application.Parsers;
using Xunit;

namespace OCAP.Knowledge.Tests;

/// <summary>Los documentos que suben las organizaciones se leen de verdad: Word, Excel, PDF y CSV.</summary>
public class DocumentReadingTests
{
    [Fact]
    public async Task Excel_IsReadSheetBySheet_WithItsHeadersOnEveryRow()
    {
        var xlsx = Zip(
            ("xl/workbook.xml", """<workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"><sheets><sheet name="Precios" sheetId="1" r:id="rId1"/></sheets></workbook>"""),
            ("xl/_rels/workbook.xml.rels", """<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="worksheet" Target="worksheets/sheet1.xml"/></Relationships>"""),
            ("xl/sharedStrings.xml", """<sst xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><si><t>Producto</t></si><si><t>Precio</t></si><si><t>Empanada</t></si><si><t>Arepa</t></si></sst>"""),
            ("xl/worksheets/sheet1.xml", """
                <worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><sheetData>
                  <row r="1"><c r="A1" t="s"><v>0</v></c><c r="B1" t="s"><v>1</v></c></row>
                  <row r="2"><c r="A2" t="s"><v>2</v></c><c r="B2"><v>2500</v></c></row>
                  <row r="3"><c r="A3" t="s"><v>3</v></c><c r="C3"><v>9</v></c></row>
                </sheetData></worksheet>
                """));

        var text = (await new XlsxDocumentParser().ParseAsync(xlsx, "precios.xlsx")).Text;

        Assert.Contains("Hoja Precios. Producto: Empanada; Precio: 2500", text);
        // La celda vacía de B3 no corre la columna: el 9 queda en la tercera.
        Assert.Contains("Producto: Arepa; Columna 3: 9", text);
    }

    [Fact]
    public async Task AnInventedCellReference_DoesNotAllocateWithoutLimit()
    {
        var xlsx = Zip(
            ("xl/workbook.xml", """<workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"><sheets><sheet name="A" sheetId="1" r:id="rId1"/></sheets></workbook>"""),
            ("xl/_rels/workbook.xml.rels", """<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="worksheet" Target="worksheets/sheet1.xml"/></Relationships>"""),
            ("xl/worksheets/sheet1.xml", """<worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><sheetData><row r="1"><c r="A1" t="inlineStr"><is><t>Nombre</t></is></c></row><row r="2"><c r="ZZZZZZ2" t="inlineStr"><is><t>x</t></is></c></row></sheetData></worksheet>"""));

        var text = (await new XlsxDocumentParser().ParseAsync(xlsx, "raro.xlsx")).Text;

        Assert.True(text.Length < 1_000);
    }

    [Fact]
    public void RepeatedHeaders_CannotInflateTheText()
    {
        var header = new string('h', 100_000);
        var rows = new List<IReadOnlyList<string>> { new[] { header } };
        rows.AddRange(Enumerable.Range(0, 1_000).Select(i => (IReadOnlyList<string>)new[] { $"valor {i}" }));

        var text = TabularText.Render("Hoja", rows, maxChars: 50_000);

        Assert.True(text.Length <= 50_000);
        Assert.DoesNotContain(new string('h', 81), text);
    }

    [Fact]
    public async Task Word_KeepsParagraphsAndTables()
    {
        var docx = Zip(("word/document.xml", """
            <w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main"><w:body>
              <w:p><w:r><w:t>Horario de atención</w:t></w:r></w:p>
              <w:p><w:r><w:t xml:space="preserve">Abrimos de lunes a </w:t></w:r><w:r><w:t>sábado.</w:t></w:r></w:p>
              <w:tbl>
                <w:tr><w:tc><w:p><w:r><w:t>Día</w:t></w:r></w:p></w:tc><w:tc><w:p><w:r><w:t>Hora</w:t></w:r></w:p></w:tc></w:tr>
                <w:tr><w:tc><w:p><w:r><w:t>Domingo</w:t></w:r></w:p></w:tc><w:tc><w:p><w:r><w:t>Cerrado</w:t></w:r></w:p></w:tc></w:tr>
              </w:tbl>
            </w:body></w:document>
            """));

        var text = (await new DocxDocumentParser().ParseAsync(docx, "horario.docx")).Text;

        Assert.Contains("Abrimos de lunes a sábado.", text);
        Assert.Contains("Día: Domingo; Hora: Cerrado", text);
        Assert.DoesNotContain("<w:", text);
    }

    [Fact]
    public async Task Pdf_TextIsExtracted()
    {
        var text = (await new PdfDocumentParser().ParseAsync(new MemoryStream(MinimalPdf("Abrimos a las 7")), "horario.pdf")).Text;

        Assert.Contains("Abrimos a las 7", text);
    }

    [Fact]
    public async Task Csv_WithSemicolonsAndQuotes_IsReadAsRows()
    {
        var csv = "﻿Producto;Descripción;Precio\nEmpanada;\"Carne; papa\";2500\n";

        var text = (await new CsvDocumentParser().ParseAsync(new MemoryStream(Encoding.UTF8.GetBytes(csv)), "precios.csv")).Text;

        Assert.Equal("Producto: Empanada; Descripción: Carne; papa; Precio: 2500", text);
    }

    [Fact]
    public void Chunks_JoinShortLines_AndSplitLongPages()
    {
        var shortLines = string.Join("\n\n", Enumerable.Range(1, 40).Select(i => $"Línea {i} con pocas palabras."));
        var longPage = string.Join(' ', Enumerable.Repeat("palabra", 1000));

        var chunks = new ParagraphChunker().ChunkDocument(shortLines + "\n\n" + longPage, chunkSize: 500);

        Assert.All(chunks, c => Assert.True(c.TokenCount <= 300, $"fragmento de {c.TokenCount} palabras"));
        Assert.True(chunks.Count < 40, "las líneas cortas se agrupan");
        Assert.Contains(chunks, c => c.Content.Contains("Línea 1 ") && c.Content.Contains("Línea 2 "));
    }

    private static MemoryStream Zip(params (string Path, string Xml)[] parts)
    {
        var stream = new MemoryStream();
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (path, xml) in parts)
            {
                using var writer = new StreamWriter(zip.CreateEntry(path).Open(), new UTF8Encoding(false));
                writer.Write(xml);
            }
        }
        stream.Position = 0;
        return stream;
    }

    /// <summary>PDF de una página con texto en Helvetica, construido a mano (con su tabla xref).</summary>
    private static byte[] MinimalPdf(string text)
    {
        var content = $"BT /F1 18 Tf 20 100 Td ({text}) Tj ET";
        var objects = new[]
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 300 144] /Contents 4 0 R /Resources << /Font << /F1 5 0 R >> >> >>",
            $"<< /Length {content.Length} >>\nstream\n{content}\nendstream",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>"
        };
        var pdf = new StringBuilder("%PDF-1.4\n");
        var offsets = new List<int>();
        for (var i = 0; i < objects.Length; i++)
        {
            offsets.Add(pdf.Length);
            pdf.Append($"{i + 1} 0 obj\n{objects[i]}\nendobj\n");
        }
        var xref = pdf.Length;
        pdf.Append($"xref\n0 {objects.Length + 1}\n0000000000 65535 f \n");
        foreach (var offset in offsets) pdf.Append($"{offset:D10} 00000 n \n");
        pdf.Append($"trailer\n<< /Size {objects.Length + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF");
        return Encoding.ASCII.GetBytes(pdf.ToString());
    }
}
