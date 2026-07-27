using OCAP.Providers.Google.Abstractions;
using OCAP.Providers.Google.Abstractions.Models;
using OCAP.Tools.Abstractions;

namespace OCAP.Tools.Google;

// Herramienta ejecutable por agentes para anexar registros en hojas de cálculo de Google Sheets.
public class AppendSpreadsheetRowTool : ITool
{
    private readonly ISpreadsheetProvider _spreadsheetProvider;

    public ToolDefinition Definition { get; } = new()
    {
        Id = "google.sheets.append_row",
        Name = "AppendSpreadsheetRowTool",
        Description = "Anexa una fila de información a una hoja de cálculo de Google Sheets.",
        Version = "1.0.0",
        RequiredPermissions = new List<string> { "Sheets.Append" },
        InputSchema = "{ \"SpreadsheetId\": \"string\", \"SheetName\": \"string\", \"Values\": [\"object\"] }",
        OutputSchema = "{ \"Status\": \"appended\" }"
    };

    public AppendSpreadsheetRowTool(ISpreadsheetProvider spreadsheetProvider)
    {
        _spreadsheetProvider = spreadsheetProvider ?? throw new ArgumentNullException(nameof(spreadsheetProvider));
    }

    public async Task<ToolResult> ExecuteAsync(ToolExecutionContext context, CancellationToken cancellationToken = default)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));

        if (!context.Parameters.TryGetValue("SpreadsheetId", out var sheetIdObj) || sheetIdObj is not string spreadsheetId || string.IsNullOrWhiteSpace(spreadsheetId))
        {
            return ToolResult.Fail("INVALID_PARAMETER", "El parámetro 'SpreadsheetId' es obligatorio.");
        }

        var sheetName = context.Parameters.TryGetValue("SheetName", out var nameObj) ? nameObj?.ToString() ?? "Sheet1" : "Sheet1";

        var values = new List<object>();
        if (context.Parameters.TryGetValue("Values", out var valObj) && valObj is IEnumerable<object> list)
        {
            values.AddRange(list);
        }

        var request = new SpreadsheetAppendRequest
        {
            SpreadsheetId = spreadsheetId,
            SheetName = sheetName,
            Values = values
        };

        var success = await _spreadsheetProvider.AppendRowAsync(request, cancellationToken);
        if (success)
        {
            return ToolResult.Ok(request, "Fila anexada exitosamente en la hoja de cálculo.");
        }

        return ToolResult.Fail("EXECUTION_ERROR", "Error al anexar la fila en Google Sheets.");
    }
}
