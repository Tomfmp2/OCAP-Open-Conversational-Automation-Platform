using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using OCAP.Security.Abstractions;

namespace OCAP.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Le dice a PostgreSQL de qué organización es cada conexión (ocap.tenant_id) para que las políticas RLS
/// (<see cref="Tenancy.RowLevelSecurity"/>) filtren las filas aunque una consulta olvide el filtro de EF o use
/// IgnoreQueryFilters. Sin organización resuelta (login, webhooks antes de identificar el canal, jobs del sistema) la
/// conexión queda en modo bypass, igual que BypassTenantFilters en EF.
/// Se fija en cada apertura: una conexión del pool nunca conserva la organización de la petición anterior.
/// </summary>
public sealed class TenantRlsConnectionInterceptor : DbConnectionInterceptor
{
    private readonly ITenantContext _tenantContext;

    public TenantRlsConnectionInterceptor(ITenantContext tenantContext)
    {
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
    }

    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        using var command = CreateCommand(connection);
        command.ExecuteNonQuery();
    }

    public override async Task ConnectionOpenedAsync(
        DbConnection connection, ConnectionEndEventData eventData, CancellationToken cancellationToken = default)
    {
        await using var command = CreateCommand(connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private DbCommand CreateCommand(DbConnection connection)
    {
        var tenantId = _tenantContext.TenantId;
        var bypass = _tenantContext.BypassTenantFilters || tenantId == Guid.Empty;

        var command = connection.CreateCommand();
        command.CommandText =
            "SELECT set_config('ocap.tenant_id', @tenant, false), set_config('ocap.bypass_rls', @bypass, false)";
        AddParameter(command, "tenant", bypass ? string.Empty : tenantId.ToString());
        AddParameter(command, "bypass", bypass ? "on" : "off");
        return command;
    }

    private static void AddParameter(DbCommand command, string name, string value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}
