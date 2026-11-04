using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OCAP.Api.Services.Memory;
using OCAP.Intelligence.Abstractions;

namespace OCAP.Api.Tests.Services;

public class MemoryBackoffTests
{
    private static readonly Guid Tenant = Guid.NewGuid();
    private static readonly DateTime Now = new(2026, 11, 4, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void CadaFalloDuplicaLaEspera_HastaUnaHora()
    {
        var backoff = new MemoryBackoff();

        backoff.Failed(Tenant, Now);
        backoff.IsPaused(Tenant, Now.AddMinutes(4)).Should().BeTrue();
        backoff.IsPaused(Tenant, Now.AddMinutes(6)).Should().BeFalse();

        backoff.Failed(Tenant, Now);
        backoff.IsPaused(Tenant, Now.AddMinutes(9)).Should().BeTrue();

        for (var i = 0; i < 10; i++) backoff.Failed(Tenant, Now);
        backoff.IsPaused(Tenant, Now.AddMinutes(59)).Should().BeTrue();
        backoff.IsPaused(Tenant, Now.AddMinutes(61)).Should().BeFalse();
    }

    [Fact]
    public void RespetaElPlazoDelProveedor()
    {
        var backoff = new MemoryBackoff();

        backoff.Failed(Tenant, Now, TimeSpan.FromHours(16));

        backoff.IsPaused(Tenant, Now.AddHours(15)).Should().BeTrue();
        backoff.IsPaused(Tenant, Now.AddHours(17)).Should().BeFalse();
    }

    [Fact]
    public void UnaOrganizacionNoPausaALasDemas()
    {
        var backoff = new MemoryBackoff();

        backoff.Failed(Tenant, Now);

        backoff.IsPaused(Guid.NewGuid(), Now).Should().BeFalse();
    }

    [Fact]
    public void UnExitoQuitaLaPausa()
    {
        var backoff = new MemoryBackoff();
        backoff.Failed(Tenant, Now);

        backoff.Succeeded(Tenant);

        backoff.IsPaused(Tenant, Now).Should().BeFalse();
    }

    [Fact]
    public async Task SinCuota_ElModeloDeMemoriaPausaHastaQueSeRenueve()
    {
        var provider = new Mock<IAiProvider>();
        provider.Setup(p => p.GenerateResponseAsync(It.IsAny<AiRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AiProviderException("Gemini", "flash", HttpStatusCode.TooManyRequests, "cuota", TimeSpan.FromHours(16)));
        var configurations = new Mock<IAiProviderConfigurationService>();
        configurations.Setup(c => c.GetRuntimeProviderForTenantAsync(Tenant, null, It.IsAny<CancellationToken>())).ReturnsAsync(provider.Object);
        var backoff = new MemoryBackoff();
        var model = new MemoryModel(configurations.Object, NullLogger<MemoryModel>.Instance, backoff);

        var summary = await model.SummarizeAsync(Tenant, null, [new MemoryTurn(true, "hola")], CancellationToken.None);

        summary.Should().BeNull();
        backoff.IsPaused(Tenant, DateTime.UtcNow.AddHours(15)).Should().BeTrue();
    }
}
