namespace OCAP.Agents.Abstractions.Providers;

public enum MessageRole
{
    System,
    User,
    Assistant,
    Tool
}

public class PromptMessage
{
    public MessageRole Role { get; }
    public string Content { get; }
    public IReadOnlyList<PromptAttachment> Attachments { get; }

    public PromptMessage(MessageRole role, string content, IReadOnlyList<PromptAttachment>? attachments = null)
    {
        Role = role;
        Content = content ?? string.Empty;
        Attachments = attachments ?? [];
    }

    public static PromptMessage System(string content) => new(MessageRole.System, content);
    public static PromptMessage User(string content, IReadOnlyList<PromptAttachment>? attachments = null) => new(MessageRole.User, content, attachments);
    public static PromptMessage Assistant(string content) => new(MessageRole.Assistant, content);
    public static PromptMessage Tool(string content) => new(MessageRole.Tool, content);
}

/// <summary>Imagen o video adjunto a un mensaje, en memoria solo durante la petición.</summary>
public sealed record PromptAttachment(string MimeType, byte[] Data);
