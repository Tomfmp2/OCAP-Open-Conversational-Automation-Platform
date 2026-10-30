namespace OCAP.Security.Abstractions;

/// <summary>
/// Fija la organización de un scope de DI que no viene de una petición con sesión (p. ej. un mensaje que trae un
/// job en segundo plano). Desde la llamada, <see cref="ITenantContext"/> de ese scope devuelve esa organización y los
/// filtros por tenant de EF vuelven a aplicarse.
/// </summary>
public interface ITenantScope
{
    void Use(Guid tenantId);
}
