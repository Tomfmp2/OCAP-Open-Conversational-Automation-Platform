using System.Net;
using System.Net.Mail;
using OCAP.Security.Abstractions;

namespace OCAP.Api.Team;

/// <summary>
/// SMTP del servidor (Email:Smtp). Opcional: cada organización puede configurar el suyo desde el panel, y sin
/// ninguno OCAP funciona igual (los enlaces se copian a mano).
/// </summary>
public sealed class SmtpOptions
{
    public string? Host { get; set; }
    public int Port { get; set; } = 587;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? From { get; set; }
    public bool UseSsl { get; set; } = true;

    public bool IsComplete => !string.IsNullOrWhiteSpace(Host) && !string.IsNullOrWhiteSpace(From);
}

public interface IEmailSender
{
    Task<bool> IsConfiguredAsync(Guid tenantId, CancellationToken cancellationToken);
    /// <summary>Antes de iniciar sesión no hay organización: basta con que alguna pueda enviar.</summary>
    Task<bool> IsAnyConfiguredAsync(CancellationToken cancellationToken);
    Task SendAsync(Guid tenantId, string to, string subject, string body, CancellationToken cancellationToken);
}

/// <summary>Envía con el SMTP de la organización o, si no tiene, con el del servidor.</summary>
public sealed class SmtpEmailSender : IEmailSender
{
    private readonly SmtpOptions _server;
    private readonly OrganizationEmailSettings _organizations;

    public SmtpEmailSender(Microsoft.Extensions.Options.IOptions<SmtpOptions> options, OrganizationEmailSettings organizations)
    {
        _server = options.Value;
        _organizations = organizations;
    }

    public async Task<bool> IsConfiguredAsync(Guid tenantId, CancellationToken cancellationToken) =>
        (await ResolveAsync(tenantId, cancellationToken)) is not null;

    public async Task<bool> IsAnyConfiguredAsync(CancellationToken cancellationToken) =>
        _server.IsComplete || await _organizations.AnyConfiguredAsync(cancellationToken);

    public async Task SendAsync(Guid tenantId, string to, string subject, string body, CancellationToken cancellationToken)
    {
        var options = await ResolveAsync(tenantId, cancellationToken) ?? throw new InvalidOperationException("El correo no está configurado.");
        // El host lo escribe un usuario: se vuelve a comprobar justo antes de conectar.
        if (options != _server && await OrganizationEmailSettings.ValidateHostAsync(options.Host!, cancellationToken) is { } blocked)
            throw new InvalidOperationException(blocked);

        using var client = new SmtpClient(options.Host, options.Port) { EnableSsl = options.UseSsl, Timeout = 30_000 };
        if (!string.IsNullOrWhiteSpace(options.Username))
            client.Credentials = new NetworkCredential(options.Username, options.Password);

        using var message = new MailMessage(options.From!, to, subject, body);
        await client.SendMailAsync(message, cancellationToken);
    }

    private async Task<SmtpOptions?> ResolveAsync(Guid tenantId, CancellationToken cancellationToken) =>
        (tenantId == Guid.Empty ? null : await _organizations.GetAsync(tenantId, cancellationToken)) is { IsComplete: true } own
            ? own
            : _server.IsComplete ? _server : null;
}
