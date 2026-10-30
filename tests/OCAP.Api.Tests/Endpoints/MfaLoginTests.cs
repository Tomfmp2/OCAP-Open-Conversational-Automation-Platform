using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using FluentAssertions;
using OCAP.Api.Tests.Infrastructure;

namespace OCAP.Api.Tests.Endpoints;

public class MfaLoginTests
{
    private const string Email = "admin@ocap.io";
    private const string Password = "ChangeMe_Admin_2026!";

    [Fact]
    public async Task WithMfaEnabled_TheSessionNeedsTheSecondFactor()
    {
        using var factory = new OcapApiFactory();
        var client = factory.CreateClient();

        // Activar MFA con la sesión del administrador.
        var session = (await (await Login(client, Password)).Content.ReadFromJsonAsync<LoginResult>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        var setup = (await (await client.PostAsync("/api/auth/mfa/setup", null)).Content.ReadFromJsonAsync<Setup>())!;
        (await client.PostAsJsonAsync("/api/auth/mfa/enable", new { code = Totp(setup.Secret) })).EnsureSuccessStatusCode();
        (await client.GetFromJsonAsync<Status>("/api/auth/mfa/status"))!.Enabled.Should().BeTrue();
        // Con el MFA activo, la sesión sola no puede reemplazar el segundo factor.
        (await client.PostAsync("/api/auth/mfa/setup", null)).IsSuccessStatusCode.Should().BeFalse();
        client.DefaultRequestHeaders.Authorization = null;

        // La contraseña sola ya no da sesión.
        var first = (await (await Login(client, Password)).Content.ReadFromJsonAsync<LoginResult>())!;
        first.MfaRequired.Should().BeTrue();
        first.AccessToken.Should().BeEmpty();
        first.MfaToken.Should().NotBeNullOrEmpty();

        // El desafío no sirve como token de acceso.
        using (var request = new HttpRequestMessage(HttpMethod.Get, "/api/inbox"))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", first.MfaToken);
            (await client.SendAsync(request)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        (await client.PostAsJsonAsync("/api/auth/mfa/verify", new { mfaToken = first.MfaToken, code = "000000" }))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var verified = await client.PostAsJsonAsync("/api/auth/mfa/verify", new { mfaToken = first.MfaToken, code = Totp(setup.Secret) });
        verified.StatusCode.Should().Be(HttpStatusCode.OK);
        var full = (await verified.Content.ReadFromJsonAsync<LoginResult>())!;
        full.AccessToken.Should().NotBeNullOrEmpty();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", full.AccessToken);
        (await client.GetAsync("/api/inbox")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AForgedChallenge_IsRejected()
    {
        using var factory = new OcapApiFactory();
        var client = factory.CreateClient();

        (await client.PostAsJsonAsync("/api/auth/mfa/verify", new { mfaToken = "no-es-un-token", code = "123456" }))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private static Task<HttpResponseMessage> Login(HttpClient client, string password) =>
        client.PostAsJsonAsync("/api/auth/login", new { email = Email, password });

    // RFC 6238 (SHA-1, 6 dígitos, 30 s), independiente del TotpService que se prueba.
    private static string Totp(string base32Secret)
    {
        var key = Base32Decode(base32Secret);
        var counter = BitConverter.GetBytes(DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30);
        if (BitConverter.IsLittleEndian) Array.Reverse(counter);
        var hash = HMACSHA1.HashData(key, counter);
        var offset = hash[^1] & 0x0F;
        var binary = ((hash[offset] & 0x7F) << 24) | (hash[offset + 1] << 16) | (hash[offset + 2] << 8) | hash[offset + 3];
        return (binary % 1_000_000).ToString("D6");
    }

    private static byte[] Base32Decode(string input)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var bits = 0;
        var value = 0;
        var output = new List<byte>();
        foreach (var c in input.TrimEnd('=').ToUpperInvariant())
        {
            value = (value << 5) | alphabet.IndexOf(c);
            bits += 5;
            if (bits >= 8)
            {
                output.Add((byte)(value >> (bits - 8)));
                bits -= 8;
            }
        }
        return output.ToArray();
    }

    private sealed record LoginResult(string AccessToken, bool MfaRequired, string? MfaToken);
    private sealed record Setup(string Secret, string QrCodeUri);
    private sealed record Status(bool Enabled, int RecoveryCodesLeft);
}
