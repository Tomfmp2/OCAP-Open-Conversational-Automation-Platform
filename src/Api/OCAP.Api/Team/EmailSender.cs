using System.Net;
using System.Net.Mail;

namespace OCAP.Api.Team;

/// <summary>Configuración SMTP opcional (Email:Smtp). Sin Host, OCAP funciona igual: los enlaces se copian a mano.</summary>
public sealed class SmtpOptions
{
    public string? Host { get; set; }
    public int Port { get; set; } = 587;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? From { get; set; }
    public bool UseSsl { get; set; } = true;
}

public interface IEmailSender
{
    bool IsConfigured { get; }
    Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken);
}

public sealed class SmtpEmailSender : IEmailSender
{
    private readonly SmtpOptions _options;

    public SmtpEmailSender(Microsoft.Extensions.Options.IOptions<SmtpOptions> options) => _options = options.Value;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.Host) && !string.IsNullOrWhiteSpace(_options.From);

    public async Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken)
    {
        if (!IsConfigured) throw new InvalidOperationException("El correo no está configurado.");

        using var client = new SmtpClient(_options.Host, _options.Port) { EnableSsl = _options.UseSsl };
        if (!string.IsNullOrWhiteSpace(_options.Username))
            client.Credentials = new NetworkCredential(_options.Username, _options.Password);

        using var message = new MailMessage(_options.From!, to, subject, body);
        await client.SendMailAsync(message, cancellationToken);
    }
}
