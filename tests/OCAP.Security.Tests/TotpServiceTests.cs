using OCAP.Security.Infrastructure.Services;
using Xunit;

namespace OCAP.Security.Tests;

public class TotpServiceTests
{
    [Fact]
    public void GenerateSecretKey_ReturnsValidBase32String()
    {
        var totpService = new TotpService();
        var secret = totpService.GenerateSecretKey();

        Assert.NotNull(secret);
        Assert.True(secret.Length >= 16);
    }

    [Fact]
    public void GenerateQrCodeUri_ReturnsValidOtpAuthUri()
    {
        var totpService = new TotpService();
        var secret = totpService.GenerateSecretKey();
        var uri = totpService.GenerateQrCodeUri("user@ocap.io", secret, "OCAP");

        Assert.StartsWith("otpauth://totp/OCAP:user%40ocap.io?secret=", uri);
        Assert.Contains("&issuer=OCAP", uri);
    }

    [Fact]
    public void ValidateCode_ValidatesCorrectlyWithinTimeWindow()
    {
        var totpService = new TotpService();
        var secret = totpService.GenerateSecretKey();

        // Invalid code should return false
        Assert.False(totpService.ValidateCode(secret, "000000"));
        Assert.False(totpService.ValidateCode(secret, "invalid"));
    }
}
