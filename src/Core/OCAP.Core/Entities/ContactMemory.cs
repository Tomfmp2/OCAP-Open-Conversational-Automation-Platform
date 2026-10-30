namespace OCAP.Core.Entities;

public enum MemoryOwnerKind
{
    /// <summary>Contacto de un canal (cliente de WhatsApp, Telegram, WebChat).</summary>
    Contact,
    /// <summary>Usuario del panel que habla con el asistente.</summary>
    PanelUser
}

public enum MemoryKind
{
    /// <summary>Dato duradero con clave (ciudad, alergias, pedido frecuente).</summary>
    Fact,
    /// <summary>Resumen de una conversación pasada.</summary>
    Episode
}

/// <summary>
/// Algo que el agente recuerda de una persona entre conversaciones. Los hechos con la misma clave no se sobrescriben:
/// el anterior queda marcado como reemplazado, así se conserva el historial y no depende de que el modelo decida qué
/// borrar. Caduca a los días que elige la organización desde la última vez que se confirmó.
/// </summary>
public class ContactMemory
{
    public const int MaxTextLength = 500;
    public const int MaxKeyLength = 60;

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public MemoryOwnerKind OwnerKind { get; private set; }
    public Guid OwnerId { get; private set; }
    public MemoryKind Kind { get; private set; }
    /// <summary>Solo hechos: "ciudad", "alergias"… en minúsculas.</summary>
    public string? Key { get; private set; }
    public string Text { get; private set; } = string.Empty;
    public Guid? SourceConversationId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime ConfirmedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime? SupersededAt { get; private set; }

    public bool IsActive(DateTime now) => SupersededAt is null && ExpiresAt > now;

    private ContactMemory() { }

    public ContactMemory(Guid tenantId, MemoryOwnerKind ownerKind, Guid ownerId, MemoryKind kind, string? key, string text,
        Guid? sourceConversationId, DateTime now, TimeSpan lifetime)
    {
        Id = Guid.NewGuid();
        TenantId = tenantId;
        OwnerKind = ownerKind;
        OwnerId = ownerId;
        Kind = kind;
        Key = NormalizeKey(key);
        Text = Clip(text);
        SourceConversationId = sourceConversationId;
        CreatedAt = ConfirmedAt = now;
        ExpiresAt = now + lifetime;
    }

    /// <summary>El dato se volvió a mencionar: sigue vigente otro periodo completo.</summary>
    public void Confirm(DateTime now, TimeSpan lifetime)
    {
        ConfirmedAt = now;
        ExpiresAt = now + lifetime;
    }

    public void Supersede(DateTime now) => SupersededAt = now;

    /// <summary>Corrección hecha a mano desde el panel.</summary>
    public void Edit(string text, DateTime now)
    {
        Text = Clip(text);
        ConfirmedAt = now;
    }

    public static string? NormalizeKey(string? key)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;
        var clean = key.Trim().ToLowerInvariant().Replace(' ', '_');
        return clean.Length > MaxKeyLength ? clean[..MaxKeyLength] : clean;
    }

    private static string Clip(string text)
    {
        var clean = text.Trim().ReplaceLineEndings(" ");
        return clean.Length > MaxTextLength ? clean[..MaxTextLength] : clean;
    }
}
