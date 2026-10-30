using System.Net;
using FluentAssertions;
using OCAP.Security.Abstractions;

namespace OCAP.Security.Tests;

public class OutboundUrlGuardTests
{
    [Theory]
    [InlineData("169.254.169.254")]   // metadata de la nube
    [InlineData("0.0.0.0")]
    [InlineData("224.0.0.1")]
    [InlineData("::ffff:169.254.169.254")]
    [InlineData("fe80::1")]
    public void AlwaysBlocked_EvenForLocalProviders(string ip)
    {
        OutboundUrlGuard.IsBlocked(IPAddress.Parse(ip), allowPrivateNetwork: true).Should().BeTrue();
    }

    [Theory]
    [InlineData("10.0.0.5")]
    [InlineData("172.18.0.3")]
    [InlineData("192.168.1.20")]
    [InlineData("100.64.0.1")]
    [InlineData("127.0.0.1")]
    [InlineData("fd00::1")]
    public void PrivateNetworks_AreBlockedForInternetDestinations(string ip)
    {
        OutboundUrlGuard.IsBlocked(IPAddress.Parse(ip), allowPrivateNetwork: false).Should().BeTrue();
    }

    [Theory]
    [InlineData("8.8.8.8")]
    [InlineData("2606:4700:4700::1111")]
    public void PublicAddresses_AreAllowed(string ip)
    {
        OutboundUrlGuard.IsBlocked(IPAddress.Parse(ip), allowPrivateNetwork: false).Should().BeFalse();
    }

    [Fact]
    public void ALanOllama_IsAllowedForLocalProviders()
    {
        OutboundUrlGuard.IsBlocked(IPAddress.Parse("192.168.1.20"), allowPrivateNetwork: true).Should().BeFalse();
    }

    [Theory]
    [InlineData("ftp://8.8.8.8/x")]
    [InlineData("http://user:pass@8.8.8.8/")]
    [InlineData("no-es-una-url")]
    [InlineData("http://169.254.169.254/latest/meta-data")]
    public async Task InvalidOrInternalUrls_AreRejected(string url)
    {
        (await OutboundUrlGuard.ValidateAsync(url, allowPrivateNetwork: false)).Should().NotBeNull();
    }

    [Fact]
    public async Task APublicUrl_IsAccepted()
    {
        (await OutboundUrlGuard.ValidateAsync("https://8.8.8.8/hook", allowPrivateNetwork: false)).Should().BeNull();
    }
}
