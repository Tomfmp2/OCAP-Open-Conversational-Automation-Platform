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
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public InstallerEndpointTests(OcapApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Status_IsAnonymous_AndReturnsPayload()
    {
        var response = await _client.GetAsync("/api/installer/status");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var status = await response.Content.ReadFromJsonAsync<InstallerStatusResponse>(JsonOptions);
        status.Should().NotBeNull();
        status!.HasAdminUsers.Should().BeTrue();
    }

    [Fact]
    public async Task Setup_WithInvalidPayload_ReturnsBadRequest()
    {
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
    public async Task Setup_WithValidLocalPayload_Succeeds()
    {
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

        var body = await response.Content.ReadFromJsonAsync<InstallerSetupResponse>(JsonOptions);
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.EnvFilePreview.Should().Contain("BOOTSTRAP_ADMIN_EMAIL=installer-admin@ocap.io");
        body.EnvFilePreview.Should().Contain("Google__ClientId=test-client-id.apps.googleusercontent.com");
        body.Status.Completed.Should().BeTrue();
    }

    [Fact]
    public async Task Setup_WebRequiresPublicUrls()
    {
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
