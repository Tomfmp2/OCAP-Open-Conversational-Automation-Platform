using FluentAssertions;
using OCAP.Providers.Google.Gmail;
using OCAP.Tools.Abstractions;
using OCAP.Tools.Google;

namespace OCAP.Tools.Tests;

public class GmailToolTests
{
    [Fact]
    public async Task ExecuteAsync_WithValidParameters_SendsEmail()
    {
        // Arrange
        var provider = new InMemoryEmailProvider();
        var tool = new SendEmailTool(provider);

        var parameters = new Dictionary<string, object>
        {
            ["To"] = "cliente@ejemplo.com",
            ["Subject"] = "Notificación de OCAP",
            ["Body"] = "Este es un correo de prueba enviado por OCAP Tool System."
        };

        var context = new ToolExecutionContext(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), parameters);

        // Act
        var result = await tool.ExecuteAsync(context);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("éxito");

        var sentEmails = await provider.GetEmailsAsync();
        sentEmails.Should().HaveCount(1);
        sentEmails.First().To.Should().Be("cliente@ejemplo.com");
    }
}
