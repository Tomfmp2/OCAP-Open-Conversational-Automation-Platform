using System.Text;

namespace OCAP.Knowledge.Application.Parsers;

/// <summary>
/// Convierte una tabla (Excel, CSV) en texto que un agente entiende fila por fila: cada fila repite los encabezados
/// ("Producto: Empanada; Precio: 2500"), así un fragmento suelto sigue diciendo qué es cada valor.
/// </summary>
public static class TabularText
{
    /// <summary>Filas por bloque; los bloques se separan con una línea en blanco para trocear sin partir filas.</summary>
    private const int RowsPerBlock = 15;
    /// <summary>Topes por valor: al repetirse en cada fila, un encabezado enorme multiplicaría el texto.</summary>
    private const int MaxHeaderChars = 80;
    private const int MaxCellChars = 500;
    public const int DefaultMaxChars = 2_000_000;

    /// <param name="title">Prefijo de cada fila (p. ej. el nombre de la hoja).</param>
    /// <param name="rows">Primera fila: encabezados.</param>
    /// <param name="maxChars">Texto máximo que se genera; las filas que no caben se descartan.</param>
    public static string Render(string? title, IReadOnlyList<IReadOnlyList<string>> rows, int maxChars = DefaultMaxChars)
    {
        var nonEmpty = rows.Where(r => r.Any(c => !string.IsNullOrWhiteSpace(c))).ToList();
        if (nonEmpty.Count == 0) return string.Empty;

        var headers = nonEmpty[0].Select((h, i) => string.IsNullOrWhiteSpace(h) ? $"Columna {i + 1}" : Clip(h.Trim(), MaxHeaderChars)).ToList();
        var output = new StringBuilder();
        var prefix = string.IsNullOrWhiteSpace(title) ? string.Empty : $"{Clip(title.Trim(), MaxHeaderChars)}. ";
        if (nonEmpty.Count == 1)
            return prefix + string.Join("; ", headers);

        var inBlock = 0;
        foreach (var row in nonEmpty.Skip(1))
        {
            if (output.Length >= maxChars) break;
            var cells = row.Select((value, i) => (Header: i < headers.Count ? headers[i] : $"Columna {i + 1}", Value: Clip(value.Trim(), MaxCellChars)))
                .Where(c => c.Value.Length > 0)
                .Select(c => $"{c.Header}: {c.Value}");
            output.Append(prefix).Append(string.Join("; ", cells)).Append('\n');
            if (++inBlock == RowsPerBlock)
            {
                output.Append('\n');
                inBlock = 0;
            }
        }
        return (output.Length > maxChars ? output.ToString(0, maxChars) : output.ToString()).TrimEnd();
    }

    private static string Clip(string value, int max) => value.Length <= max ? value : value[..max];

    /// <summary>CSV con comillas (RFC 4180) y separador "," o ";" (Excel en español exporta con ";").</summary>
    public static List<IReadOnlyList<string>> ParseCsv(string text)
    {
        var firstLine = text.Split('\n', 2)[0];
        var separator = firstLine.Count(c => c == ';') > firstLine.Count(c => c == ',') ? ';' : ',';
        var rows = new List<IReadOnlyList<string>>();
        var row = new List<string>();
        var cell = new StringBuilder();
        var quoted = false;
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (quoted)
            {
                if (c == '"' && i + 1 < text.Length && text[i + 1] == '"') { cell.Append('"'); i++; }
                else if (c == '"') quoted = false;
                else cell.Append(c);
            }
            else if (c == '"') quoted = true;
            else if (c == separator) { row.Add(cell.ToString()); cell.Clear(); }
            else if (c == '\n' || c == '\r')
            {
                if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n') i++;
                row.Add(cell.ToString());
                cell.Clear();
                rows.Add(row);
                row = new List<string>();
            }
            else cell.Append(c);
        }
        if (cell.Length > 0 || row.Count > 0)
        {
            row.Add(cell.ToString());
            rows.Add(row);
        }
        return rows;
    }
}
