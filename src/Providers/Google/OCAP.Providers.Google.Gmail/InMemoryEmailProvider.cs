using OCAP.Providers.Google.Abstractions;
using OCAP.Providers.Google.Abstractions.Models;

namespace OCAP.Providers.Google.Gmail;

// Implementación en memoria del proveedor de Gmail para entornos aislados y pruebas.
public class InMemoryEmailProvider : IEmailProvider
{
    private readonly List<EmailMessage> _sentEmails = new();

    public Task<EmailMessage> SendEmailAsync(EmailMessage email, CancellationToken cancellationToken = default)
    {
        if (email == null) throw new ArgumentNullException(nameof(email));
        
        _sentEmails.Add(email);
        return Task.FromResult(email);
    }

    public Task<IReadOnlyList<EmailMessage>> GetEmailsAsync(int maxResults = 10, CancellationToken cancellationToken = default)
    {
        var result = _sentEmails.Take(maxResults).ToList();
        return Task.FromResult<IReadOnlyList<EmailMessage>>(result);
    }
}
