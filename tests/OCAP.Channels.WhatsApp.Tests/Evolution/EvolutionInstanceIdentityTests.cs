using FluentAssertions;
using OCAP.Channels.WhatsApp.Evolution;

namespace OCAP.Channels.WhatsApp.Tests.Evolution;

public class EvolutionInstanceIdentityTests
{
    private const string Secret = "webhook-secret-de-prueba";

    [Fact]
    public void InstanceName_RoundTripsToTheSameTenant()
    {
        var tenantId = Guid.NewGuid();

        var instance = EvolutionInstanceIdentity.InstanceNameFor(tenantId);

        EvolutionInstanceIdentity.TryParseTenant(instance, out var parsed).Should().BeTrue();
        parsed.Should().Be(tenantId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("ocap-main")]
    [InlineData("otra-00000000000000000000000000000001")]
    [InlineData("ocap-00000000000000000000000000000000")]
    public void TryParseTenant_RejectsNamesThatAreNotAnOrganizationInstance(string? instance)
    {
        EvolutionInstanceIdentity.TryParseTenant(instance, out _).Should().BeFalse();
    }

    [Fact]
    public void Signature_IsValidOnlyForItsOwnInstance()
    {
        var mine = EvolutionInstanceIdentity.InstanceNameFor(Guid.NewGuid());
        var other = EvolutionInstanceIdentity.InstanceNameFor(Guid.NewGuid());
        var signature = EvolutionInstanceIdentity.SignatureFor(mine, Secret);

        EvolutionInstanceIdentity.IsValidSignature(mine, signature, Secret).Should().BeTrue();
        EvolutionInstanceIdentity.IsValidSignature(mine, signature.ToUpperInvariant(), Secret).Should().BeTrue();
        EvolutionInstanceIdentity.IsValidSignature(other, signature, Secret).Should().BeFalse();
        EvolutionInstanceIdentity.IsValidSignature(mine, signature, "otro-secreto").Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("abc")]
    public void Signature_MissingOrMalformed_IsRejected(string? signature)
    {
        var instance = EvolutionInstanceIdentity.InstanceNameFor(Guid.NewGuid());

        EvolutionInstanceIdentity.IsValidSignature(instance, signature, Secret).Should().BeFalse();
    }

    [Fact]
    public void Signature_WithoutSecret_IsRejected()
    {
        var instance = EvolutionInstanceIdentity.InstanceNameFor(Guid.NewGuid());

        EvolutionInstanceIdentity.IsValidSignature(instance, EvolutionInstanceIdentity.SignatureFor(instance, "x"), "").Should().BeFalse();
    }
}
