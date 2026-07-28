using FluentAssertions;
using OCAP.Channels.Telegram.Services;

namespace OCAP.Channels.Tests.Telegram;

public class TelegramArchitectureBoundaryTests
{
    [Fact]
    public void TelegramAdapterAssembly_ShouldNotReferenceBusinessLogicOrDatabaseAssemblies()
    {
        // Arrange
        var assembly = typeof(TelegramChannelProvider).Assembly;
        var referencedAssemblies = assembly.GetReferencedAssemblies().Select(a => a.Name).ToList();

        // Assert — Hexagonal Architecture Boundaries Verification
        referencedAssemblies.Should().NotContain("OCAP.Intelligence");
        referencedAssemblies.Should().NotContain("OCAP.Knowledge");
        referencedAssemblies.Should().NotContain("OCAP.Workflow");
        referencedAssemblies.Should().NotContain("OCAP.Infrastructure");
        referencedAssemblies.Should().NotContain("Microsoft.EntityFrameworkCore");
    }
}
