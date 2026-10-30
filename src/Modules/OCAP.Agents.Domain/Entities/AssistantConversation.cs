namespace OCAP.Agents.Domain.Entities;

/// <summary>
/// Conversación de un usuario con el asistente del panel. Es suya: otros usuarios de la organización no la ven.
/// </summary>
public class AssistantConversation
{
    public const int TitleMaxLength = 120;

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid UserId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    /// <summary>
    /// Tuvo archivos adjuntos: lo que el modelo leyó de ellos sigue en el historial, así que ningún turno de esta
    /// conversación ejecuta herramientas.
    /// </summary>
    public bool HasUntrustedContent { get; private set; }

    private AssistantConversation() { }

    public AssistantConversation(Guid tenantId, Guid userId, string firstMessage)
    {
        Id = Guid.NewGuid();
        TenantId = tenantId;
        UserId = userId;
        Title = TitleFrom(firstMessage);
        CreatedAt = UpdatedAt = DateTime.UtcNow;
    }

    public void Rename(string title)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("El título no puede estar vacío.", nameof(title));
        Title = TitleFrom(title);
    }

    public void Touch() => UpdatedAt = DateTime.UtcNow;

    public void MarkUntrustedContent() => HasUntrustedContent = true;

    private static string TitleFrom(string text)
    {
        var line = text.Trim().ReplaceLineEndings(" ");
        return line.Length <= TitleMaxLength ? line : line[..(TitleMaxLength - 1)].TrimEnd() + "…";
    }
}

public static class AssistantRoles
{
    public const string User = "user";
    public const string Assistant = "assistant";
}

public class AssistantMessage
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid ConversationId { get; private set; }
    /// <summary>Uno de <see cref="AssistantRoles"/>.</summary>
    public string Role { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    private AssistantMessage() { }

    /// <param name="conversation">Conversación.</param>
    /// <param name="role">Uno de <see cref="AssistantRoles"/>.</param>
    /// <param name="content">Texto.</param>
    /// <param name="createdAt">Cuándo se escribió; el mensaje del usuario usa la hora de llegada, no la de guardado, para
    /// quedar antes que la respuesta aunque ambos se guarden juntos.</param>
    public AssistantMessage(AssistantConversation conversation, string role, string content, DateTime? createdAt = null)
    {
        Id = Guid.NewGuid();
        TenantId = conversation.TenantId;
        ConversationId = conversation.Id;
        Role = role;
        Content = content;
        CreatedAt = createdAt ?? DateTime.UtcNow;
    }
}
