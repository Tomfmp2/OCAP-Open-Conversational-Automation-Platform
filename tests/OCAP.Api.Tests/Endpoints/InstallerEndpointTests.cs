using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using OCAP.Api.Installation;
using OCAP.Api.Tests.Infrastructure;

namespace OCAP.Api.Tests.Endpoints;

public class InstallerEndpointTests : IClassFixture<OcapApiFactory>
{
    private readonly HttpClient _client;
    private readonly OcapApiFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public InstallerEndpointTests(OcapApiFactory factory)
    {
        _factory = factory;
        _factory.ResetInstallationArtifacts();
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Status_IsAnonymous_AndReturnsPayload()
    {
        _factory.ResetInstallationArtifacts();
        var response = await _client.GetAsync("/api/installer/status");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var status = await response.Content.ReadFromJsonAsync<InstallerStatusResponse>(JsonOptions);
        status.Should().NotBeNull();
        status!.HasAdminUsers.Should().BeTrue();
        status.AllowsAnonymousSetup.Should().BeTrue();
    }

    [Fact]
    public async Task Setup_WithInvalidPayload_ReturnsBadRequest()
    {
        _factory.ResetInstallationArtifacts();
        var payload = new InstallerSetupRequest
        {
            Target = "Local",
            FrontendHostPort = 3000,
            ApiHostPort = 5000,
            PostgresHost = "localhost",
            PostgresPort = 5433,
            PostgresDbName = "ocap_db",
            PostgresUsername = "ocap_user",
            PostgresPassword = "short",
            AdminEmail = "not-an-email",
            AdminPassword = "short",
            TenantName = "Test",
            TenantSlug = "INVALID SLUG",
            EnableGoogleWorkspace = true,
            GoogleClientId = "",
            GoogleClientSecret = "",
            AiProvider = "OpenAI",
            AiApiKey = "",
            AiModelName = "gpt-4o"
        };

        var response = await _client.PostAsJsonAsync("/api/installer/setup", payload);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Setup_WithValidLocalPayload_Succeeds_WithoutSecretPreview()
    {
        _factory.ResetInstallationArtifacts();
        var payload = new InstallerSetupRequest
        {
            Target = "Local",
            FrontendHostPort = 3000,
            ApiHostPort = 5000,
            PostgresHost = "localhost",
            PostgresPort = 5433,
            PostgresDbName = "ocap_db",
            PostgresUsername = "ocap_user",
            PostgresPassword = "OcapSecurePass2026!",
            AdminEmail = "installer-admin@ocap.io",
            AdminPassword = "Installer_Admin_2026!",
            TenantName = "Installer Org",
            TenantSlug = "installer-org",
            EnableGoogleWorkspace = true,
            GoogleClientId = "test-client-id.apps.googleusercontent.com",
            GoogleClientSecret = "test-client-secret",
            AiProvider = "OpenAI",
            AiApiKey = "sk-test-key",
            AiModelName = "gpt-4o",
            EnableWhatsApp = false,
            EnableTelegram = false
        };

        var response = await _client.PostAsJsonAsync("/api/installer/setup", payload);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var raw = await response.Content.ReadAsStringAsync();
        var body = JsonSerializer.Deserialize<InstallerSetupResponse>(raw, JsonOptions);
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.EnvKeysUpdated.Should().NotBeEmpty();
        body.EnvKeysUpdated.Should().Contain("BOOTSTRAP_ADMIN_EMAIL");
        raw.Should().NotContain("sk-test-key");
        raw.Should().NotContain("test-client-secret");
        body.Status.Completed.Should().BeTrue();
    }

    [Fact]
    public async Task Setup_Dev_AllowsEmptyAiKey()
    {
        _factory.ResetInstallationArtifacts();
        var payload = new InstallerSetupRequest
        {
            Target = "Dev",
            AdminEmail = "dev-admin@ocap.io",
            AdminPassword = "Dev_Admin_2026!",
            TenantName = "OCAP Local",
            TenantSlug = "local",
            EnableGoogleWorkspace = false,
            AiProvider = "Gemini",
            AiApiKey = "",
            AiModelName = "gemini-3.5-flash"
        };

        var response = await _client.PostAsJsonAsync("/api/installer/setup", payload);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<InstallerSetupResponse>(JsonOptions);
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Status.Target.Should().Be("Dev");
        body.Status.ApiHostPort.Should().Be(5229);
    }

    [Fact]
    public async Task Setup_AfterCompleted_Anonymous_ReturnsForbidden()
    {
        _factory.ResetInstallationArtifacts();
        var payload = new InstallerSetupRequest
        {
            Target = "Dev",
            AdminEmail = "first@ocap.io",
            AdminPassword = "First_Admin_2026!",
            TenantName = "OCAP Local",
            TenantSlug = "local",
            EnableGoogleWorkspace = false,
            AiProvider = "Gemini",
            AiModelName = "gemini-3.5-flash"
        };

        var first = await _client.PostAsJsonAsync("/api/installer/setup", payload);
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        payload.AdminEmail = "second@ocap.io";
        var second = await _client.PostAsJsonAsync("/api/installer/setup", payload);
        second.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Setup_WebRequiresPublicUrls()
    {
        _factory.ResetInstallationArtifacts();
        var payload = new InstallerSetupRequest
        {
            Target = "Web",
            PublicApiUrl = "",
            PublicPanelUrl = "",
            PostgresHost = "localhost",
            PostgresPort = 5433,
            PostgresDbName = "ocap_db",
            PostgresUsername = "ocap_user",
            PostgresPassword = "OcapSecurePass2026!",
            AdminEmail = "admin@example.com",
            AdminPassword = "SecurePass_2026!",
            TenantName = "Web Org",
            TenantSlug = "web-org",
            EnableGoogleWorkspace = false,
            AiProvider = "Ollama",
            AiModelName = "llama3",
            AiBaseUrl = "http://localhost:11434"
        };

        var response = await _client.PostAsJsonAsync("/api/installer/setup", payload);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
