using OCAP.Providers.Google.Abstractions;
using OCAP.Tools.Abstractions;

namespace OCAP.Tools.Google;

/// <summary>Lista correos recientes de Gmail (o bandeja in-memory en desarrollo).</summary>
public class ListEmailsTool : ITool
{
    private readonly IEmailProvider _emailProvider;

    public ToolDefinition Definition { get; } = new()
    {
        Id = "google.gmail.list_emails",
        Name = "ListEmailsTool",
        Description = "Lista los correos más recientes de la bandeja de Gmail.",
        Version = "1.0.0",
        RequiredPermissions = new List<string> { "Gmail.Read" },
        InputSchema = "{ \"MaxResults\": \"number (opcional, default 5)\" }",
        OutputSchema = "{ \"Emails\": [ { \"To\", \"Subject\", \"Body\" } ] }"
    };

    public ListEmailsTool(IEmailProvider emailProvider)
    {
        _emailProvider = emailProvider ?? throw new ArgumentNullException(nameof(emailProvider));
    }

    public async Task<ToolResult> ExecuteAsync(ToolExecutionContext context, CancellationToken cancellationToken = default)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));

        var max = 5;
        if (context.Parameters.TryGetValue("MaxResults", out var maxObj))
        {
            if (maxObj is int i) max = i;
            else if (maxObj is long l) max = (int)l;
            else if (maxObj is double d) max = (int)d;
            else if (int.TryParse(maxObj?.ToString(), out var parsed)) max = parsed;
        }

        max = Math.Clamp(max, 1, 50);
        var emails = await _emailProvider.GetEmailsAsync(max, cancellationToken);
        return ToolResult.Ok(emails, $"Se listaron {emails.Count} correo(s).");
    }
}
