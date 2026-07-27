using OCAP.Providers.Google.Abstractions;
using OCAP.Providers.Google.Abstractions.Models;
using OCAP.Tools.Abstractions;

namespace OCAP.Tools.Google;

// Herramienta ejecutable por agentes para el envío de correos electrónicos vía Gmail.
public class SendEmailTool : ITool
{
    private readonly IEmailProvider _emailProvider;

    public ToolDefinition Definition { get; } = new()
    {
        Id = "google.gmail.send_email",
        Name = "SendEmailTool",
        Description = "Envía un correo electrónico a través de Gmail.",
        Version = "1.0.0",
        RequiredPermissions = new List<string> { "Gmail.Send" },
        InputSchema = "{ \"To\": \"string\", \"Subject\": \"string\", \"Body\": \"string\" }",
        OutputSchema = "{ \"EmailId\": \"string\", \"Status\": \"sent\" }"
    };

    public SendEmailTool(IEmailProvider emailProvider)
    {
        _emailProvider = emailProvider ?? throw new ArgumentNullException(nameof(emailProvider));
    }

    public async Task<ToolResult> ExecuteAsync(ToolExecutionContext context, CancellationToken cancellationToken = default)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));

        if (!context.Parameters.TryGetValue("To", out var toObj) || toObj is not string to || string.IsNullOrWhiteSpace(to))
        {
            return ToolResult.Fail("INVALID_PARAMETER", "El parámetro 'To' es obligatorio.");
        }

        var subject = context.Parameters.TryGetValue("Subject", out var subjObj) ? subjObj?.ToString() ?? string.Empty : string.Empty;
        var body = context.Parameters.TryGetValue("Body", out var bodyObj) ? bodyObj?.ToString() ?? string.Empty : string.Empty;

        var email = new EmailMessage
        {
            To = to,
            Subject = subject,
            Body = body
        };

        var sent = await _emailProvider.SendEmailAsync(email, cancellationToken);
        return ToolResult.Ok(sent, "Correo electrónico enviado con éxito.");
    }
}
