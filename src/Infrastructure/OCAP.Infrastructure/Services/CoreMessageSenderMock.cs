using Microsoft.Extensions.Logging;
using OCAP.Core.Entities;
using OCAP.Core.Ports;

namespace OCAP.Infrastructure.Services;

public class CoreMessageSenderMock : IMessageSender
{
    private readonly ILogger<CoreMessageSenderMock> _logger;

    public CoreMessageSenderMock(ILogger<CoreMessageSenderMock> logger)
    {
        _logger = logger;
    }

    public Task SendMessageAsync(Message message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("CoreMessageSenderMock: Envió el mensaje {MessageId} de forma simulada.", message?.Id);
        return Task.CompletedTask;
    }
}
