using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using OCAP.Agents.Abstractions.Providers;
using OCAP.Agents.Application.Services;
using OCAP.Infrastructure.Persistence.Context;
using OCAP.Infrastructure.Services;
using OCAP.Intelligence.Abstractions;
using OCAP.Intelligence.Application.Services;
using OCAP.Intelligence.Domain;
using OCAP.Providers.Ollama;
using OCAP.Providers.OpenAI;
using OCAP.Security.Infrastructure.Services;
using Xunit;

namespace OCAP.Intelligence.Tests;

public class AiProviderRuntimeAndConfigurationTests
{
    private OCAPDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<OCAPDbContext>()
            .UseInMemoryDatabase(databaseName: $"OCAP_TestDb_{Guid.NewGuid():N}")
            .Options;

        return new OCAPDbContext(options);
    }

    [Fact]
    public void AiProviderRegistry_ShouldRegisterAndResolveProvidersCorrectly()
    {
        // Arrange
        var httpClient = new HttpClient();
        var openAiSettings = new AiProviderSettings { ApiKey = "mock-key", ModelName = "gpt-4o" };
        var openAiProvider = new OpenAiProvider(httpClient, openAiSettings);
        var localProvider = new LocalAiProvider(httpClient, openAiSettings);

        var registry = new AiProviderRegistry(new IAiProvider[] { openAiProvider, localProvider }, httpClient);

        // Act
        var names = registry.GetRegisteredProviderNames();
        var retrievedOpenAi = registry.GetProvider("OpenAI");
        var retrievedLocal = registry.GetProvider("Local");
        var dynamicGemini = registry.CreateDynamicProvider("Gemini", "gemini-1.5-flash", "test-key");

        // Assert
        names.Should().Contain("OpenAI");
        names.Should().Contain("Local");
        retrievedOpenAi.Should().NotBeNull();
        retrievedLocal.Should().NotBeNull();
        dynamicGemini.Should().NotBeNull();
        dynamicGemini.Name.Should().Be("Gemini");
    }

    [Fact]
    public async Task AiProviderConfigurationService_ShouldCreateAndUpdateConfigurationsWithEncryptedCredentials()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var vault = new AesDbCredentialVault(NullLogger<AesDbCredentialVault>.Instance, "OCAP_TESTING_VAULT_MASTER_KEY_32CHARS_MIN!");
        var httpClient = new HttpClient();
        var registry = new AiProviderRegistry(Array.Empty<IAiProvider>(), httpClient);
        var logger = NullLogger<AiProviderConfigurationService>.Instance;

        var service = new AiProviderConfigurationService(dbContext, vault, registry, logger);
        var tenantId = Guid.NewGuid();

        var createDto = new CreateAiProviderConfigurationDto(
            TenantId: tenantId,
            ProviderName: "OpenAI",
            DisplayName: "OpenAI Production",
            ModelName: "gpt-4o",
            ApiKey: "sk-secret-key-12345",
            BaseUrl: "https://api.openai.com/v1"
        );

        // Act - Create
        var created = await service.CreateConfigurationAsync(createDto);

        // Assert - Create
        created.Should().NotBeNull();
        created.TenantId.Should().Be(tenantId);
        created.ProviderName.Should().Be("OpenAI");
        created.VaultSecretReference.Should().NotBeNullOrEmpty();
        created.VaultSecretReference.Should().NotContain("sk-secret-key-12345"); // Credenciales nunca en texto plano

        // Act - Get
        var list = await service.GetConfigurationsByTenantAsync(tenantId);
        list.Should().HaveCount(1);
        list.First().Id.Should().Be(created.Id);

        // Act - Get Runtime Provider
        var runtimeProvider = await service.GetRuntimeProviderForTenantAsync(tenantId);
        runtimeProvider.Should().NotBeNull();
        runtimeProvider.Name.Should().Be("OpenAI");

        // Act - Disable
        var statusResult = await service.SetStatusAsync(tenantId, created.Id, false);
        statusResult.Should().BeTrue();

        // Fallback cuando está deshabilitado lanza excepcion
        var action = async () => await service.GetRuntimeProviderForTenantAsync(tenantId);
        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task EnterpriseAssistant_ShouldUseDynamicTenantLanguageModelProviderSelector()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var vault = new AesDbCredentialVault(NullLogger<AesDbCredentialVault>.Instance, "OCAP_TESTING_VAULT_MASTER_KEY_32CHARS_MIN!");
        var httpClient = new HttpClient(new FakeHttpMessageHandler());
        var registry = new AiProviderRegistry(Array.Empty<IAiProvider>(), httpClient);
        var logger = NullLogger<AiProviderConfigurationService>.Instance;
        var service = new AiProviderConfigurationService(dbContext, vault, registry, logger);

        var tenantId = Guid.NewGuid();
        await service.CreateConfigurationAsync(new CreateAiProviderConfigurationDto(
            TenantId: tenantId,
            ProviderName: "Local",
            DisplayName: "Local LLaMA 3",
            ModelName: "llama3-local",
            ApiKey: "local-key"
        ));

        var selector = new DefaultLanguageModelProviderSelector(Array.Empty<ILanguageModelProvider>(), service);

        // Act
        var resolvedProvider = await selector.GetProviderAsync(tenantId);

        // Assert
        resolvedProvider.Should().NotBeNull();
        resolvedProvider.ProviderName.Should().Be("Local");

        var response = await resolvedProvider.GenerateAsync(new LanguageModelRequest(new[] { new PromptMessage(MessageRole.User, "Hola OCAP") }));
        response.Content.Should().Contain("Hola OCAP");
    }

    private class FakeHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("{\"choices\": [{\"text\": \"Hola OCAP\"}]}")
            };
            return Task.FromResult(response);
        }
    }
}
