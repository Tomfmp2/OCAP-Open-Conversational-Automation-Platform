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

    public PromptMessage(MessageRole role, string content)
    {
        Role = role;
        Content = content ?? string.Empty;
    }

    public static PromptMessage System(string content) => new(MessageRole.System, content);
    public static PromptMessage User(string content) => new(MessageRole.User, content);
    public static PromptMessage Assistant(string content) => new(MessageRole.Assistant, content);
    public static PromptMessage Tool(string content) => new(MessageRole.Tool, content);
}
