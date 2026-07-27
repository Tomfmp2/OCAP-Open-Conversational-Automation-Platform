using OCAP.Providers.Google.Abstractions.Models;

namespace OCAP.Providers.Google.Abstractions;

// Contrato desacoplado para operaciones con Google Sheets sin depender de SDKs de terceros.
public interface ISpreadsheetProvider
{
    // Anexa una fila de datos a la hoja de cálculo indicada.
    Task<bool> AppendRowAsync(SpreadsheetAppendRequest request, CancellationToken cancellationToken = default);

    // Lee las filas dentro de un rango específico de la hoja de cálculo.
    Task<IReadOnlyList<IReadOnlyList<object>>> ReadRowsAsync(string spreadsheetId, string range, CancellationToken cancellationToken = default);
}
