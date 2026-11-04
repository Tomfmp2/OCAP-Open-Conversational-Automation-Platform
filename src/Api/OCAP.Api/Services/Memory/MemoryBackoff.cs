using System.Collections.Concurrent;

namespace OCAP.Api.Services.Memory;

/// <summary>
/// Pausa la memoria de una organización cuando su modelo falla. Es trabajo diferible: reintentarlo cada minuto gastaría
/// la cuota que necesitan las respuestas a los clientes. La pausa vive en memoria; al reiniciar se intenta una vez más.
/// </summary>
public sealed class MemoryBackoff
{
    private static readonly TimeSpan First = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan MaxWithoutHint = TimeSpan.FromHours(1);
    private static readonly TimeSpan MaxWithHint = TimeSpan.FromHours(24);

    private readonly ConcurrentDictionary<Guid, (int Failures, DateTime Until)> _paused = new();

    public bool IsPaused(Guid tenantId, DateTime now) =>
        _paused.TryGetValue(tenantId, out var state) && state.Until > now;

    /// <param name="retryAfter">Plazo indicado por el proveedor, si lo dio (por ejemplo, cuando se renueva la cuota).</param>
    public void Failed(Guid tenantId, DateTime now, TimeSpan? retryAfter = null) =>
        _paused.AddOrUpdate(
            tenantId,
            _ => (1, now + Wait(1, retryAfter)),
            (_, state) => (state.Failures + 1, now + Wait(state.Failures + 1, retryAfter)));

    public void Succeeded(Guid tenantId) => _paused.TryRemove(tenantId, out _);

    private static TimeSpan Wait(int failures, TimeSpan? retryAfter)
    {
        if (retryAfter is { } hint && hint > TimeSpan.Zero) return hint < MaxWithHint ? hint : MaxWithHint;
        var doubled = First * Math.Pow(2, Math.Min(failures - 1, 8));
        return doubled < MaxWithoutHint ? doubled : MaxWithoutHint;
    }
}
