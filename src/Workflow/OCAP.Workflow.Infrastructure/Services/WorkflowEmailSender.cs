using OCAP.Providers.Google.Abstractions;
using OCAP.Providers.Google.Abstractions.Models;
using OCAP.Workflow.Abstractions;

namespace OCAP.Workflow.Infrastructure.Services;

public class WorkflowEmailSender : IWorkflowEmailSender
{
    private readonly IEmailProvider _emailProvider;

    public WorkflowEmailSender(IEmailProvider emailProvider)
    {
        _emailProvider = emailProvider ?? throw new ArgumentNullException(nameof(emailProvider));
    }

    public async Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        var message = new EmailMessage
        {
            To = to,
            Subject = subject,
            Body = body
        };

        await _emailProvider.SendEmailAsync(message, cancellationToken);
    }
}
