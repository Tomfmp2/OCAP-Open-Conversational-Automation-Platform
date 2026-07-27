namespace OCAP.Providers.Google.Abstractions.Models;

// DTO para solicitar anexar una fila a Google Sheets.
public class SpreadsheetAppendRequest
{
    public string SpreadsheetId { get; set; } = string.Empty;
    public string SheetName { get; set; } = "Sheet1";
    public List<object> Values { get; set; } = new();
}
