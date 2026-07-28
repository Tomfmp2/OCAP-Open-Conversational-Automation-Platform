using FluentAssertions;
using OCAP.Channels.Abstractions.Registry;

namespace OCAP.Channels.Tests;

public class ChannelManagementArchitectureBoundaryTests
{
    [Fact]
    public void ChannelAbstractionsAssembly_ShouldNotReferenceIntelligenceWorkflowOrKnowledge()
    {
        // Arrange
        var assembly = typeof(ChannelRegistry).Assembly;
        var referencedAssemblies = assembly.GetReferencedAssemblies().Select(a => a.Name).ToList();

        // Assert — Hexagonal Architecture Boundaries Verification
        referencedAssemblies.Should().NotContain("OCAP.Intelligence");
        referencedAssemblies.Should().NotContain("OCAP.Knowledge");
        referencedAssemblies.Should().NotContain("OCAP.Workflow");
    }
}
