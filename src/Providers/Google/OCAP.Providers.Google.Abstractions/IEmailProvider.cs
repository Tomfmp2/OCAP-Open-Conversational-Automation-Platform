using OCAP.Providers.Google.Abstractions.Models;

namespace OCAP.Providers.Google.Abstractions;

// Contrato desacoplado para servicios de Gmail sin depender de SDKs de terceros.
public interface IEmailProvider
{
    // Envía un correo electrónico mediante Gmail.
    Task<EmailMessage> SendEmailAsync(EmailMessage email, CancellationToken cancellationToken = default);

    // Consulta los correos más recientes de la bandeja.
    Task<IReadOnlyList<EmailMessage>> GetEmailsAsync(int maxResults = 10, CancellationToken cancellationToken = default);
}
