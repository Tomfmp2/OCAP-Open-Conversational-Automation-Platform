namespace OCAP.Workflow.Abstractions;

public interface IWorkflowEmailSender
{
    Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default);
}
