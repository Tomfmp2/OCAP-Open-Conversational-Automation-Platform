using OCAP.Providers.Google.Abstractions;
using OCAP.Providers.Google.Abstractions.Models;

namespace OCAP.Providers.Google.Sheets;

// Implementación en memoria del proveedor de Google Sheets para entornos aislados y pruebas.
public class InMemorySpreadsheetProvider : ISpreadsheetProvider
{
    private readonly Dictionary<string, List<List<object>>> _sheetsData = new();

    public Task<bool> AppendRowAsync(SpreadsheetAppendRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));

        if (!_sheetsData.ContainsKey(request.SpreadsheetId))
        {
            _sheetsData[request.SpreadsheetId] = new List<List<object>>();
        }

        _sheetsData[request.SpreadsheetId].Add(request.Values);
        return Task.FromResult(true);
    }

    public Task<IReadOnlyList<IReadOnlyList<object>>> ReadRowsAsync(string spreadsheetId, string range, CancellationToken cancellationToken = default)
    {
        if (_sheetsData.TryGetValue(spreadsheetId, out var rows))
        {
            var casted = rows.Select(r => (IReadOnlyList<object>)r).ToList();
            return Task.FromResult<IReadOnlyList<IReadOnlyList<object>>>(casted);
        }

        return Task.FromResult<IReadOnlyList<IReadOnlyList<object>>>(new List<IReadOnlyList<object>>());
    }
}
