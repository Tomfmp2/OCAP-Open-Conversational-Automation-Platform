using System.Data;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using OCAP.Infrastructure.Persistence.Context;
using OCAP.Workflow.Abstractions;

namespace OCAP.Workflow.Infrastructure.Services;

public class WorkflowDatabaseExecutor : IWorkflowDatabaseExecutor
{
    private static readonly Regex ForbiddenKeywordPattern = new(
        @"\b(INSERT|UPDATE|DELETE|DROP|ALTER|CREATE|TRUNCATE|EXEC|EXECUTE)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly OCAPDbContext _context;

    public WorkflowDatabaseExecutor(OCAPDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<IReadOnlyList<Dictionary<string, object?>>> QueryAsync(
        Guid tenantId,
        string sql,
        IDictionary<string, object?>? parameters,
        CancellationToken cancellationToken = default)
    {
        ValidateSql(sql);

        var connection = _context.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        var tenantParam = command.CreateParameter();
        tenantParam.ParameterName = "@tenantId";
        tenantParam.Value = tenantId;
        command.Parameters.Add(tenantParam);

        if (parameters != null)
        {
            foreach (var (key, value) in parameters)
            {
                if (string.Equals(key, "@tenantId", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(key, "tenantId", StringComparison.OrdinalIgnoreCase))
                    continue;

                var param = command.CreateParameter();
                param.ParameterName = key.StartsWith('@') ? key : $"@{key}";
                param.Value = value ?? DBNull.Value;
                command.Parameters.Add(param);
            }
        }

        var rows = new List<Dictionary<string, object?>>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < reader.FieldCount; i++)
            {
                var name = reader.GetName(i);
                var value = await reader.IsDBNullAsync(i, cancellationToken) ? null : reader.GetValue(i);
                row[name] = value;
            }
            rows.Add(row);
        }

        return rows;
    }

    public static void ValidateSql(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
            throw new InvalidOperationException("SQL vacío no permitido.");

        var trimmed = sql.Trim();
        if (!trimmed.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Solo se permiten consultas SELECT.");

        if (trimmed.Contains(';'))
            throw new InvalidOperationException("No se permiten múltiples sentencias SQL.");

        if (ForbiddenKeywordPattern.IsMatch(trimmed))
            throw new InvalidOperationException("La consulta contiene palabras clave no permitidas.");
    }
}
