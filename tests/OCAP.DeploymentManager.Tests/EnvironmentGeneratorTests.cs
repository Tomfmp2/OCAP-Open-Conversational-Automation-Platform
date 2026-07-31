using FluentAssertions;
using OCAP.DeploymentManager.Models;
using OCAP.DeploymentManager.Services;

namespace OCAP.DeploymentManager.Tests;

public class EnvironmentGeneratorTests
{
    private readonly EnvironmentGenerator _generator = new();

    [Fact]
    public void GenerateEnvironmentFileContent_ReturnsExpectedVariables()
    {
        // Arrange
        var config = new DeploymentConfiguration
        {
            PostgresHost = "db.example.com",
            PostgresDbName = "custom_ocap_db",
            BootstrapAdminEmail = "admin@example.com",
            GoogleClientId = "cid.apps.googleusercontent.com",
            GoogleClientSecret = "secret",
            AiProvider = "OpenAI",
            AiApiKey = "sk-test",
            AiModelName = "gpt-4o",
            FrontendHostPort = 3100,
            ApiHostPort = 5100,
            Target = DeploymentTarget.Local
        };

        // Act
        var content = _generator.GenerateEnvironmentFileContent(config);

        // Assert
        content.Should().Contain("POSTGRES_HOST=db.example.com");
        content.Should().Contain("POSTGRES_DB=custom_ocap_db");
        content.Should().Contain("JWT_SECRET_KEY=");
        content.Should().Contain("BOOTSTRAP_ADMIN_EMAIL=admin@example.com");
        content.Should().Contain("Google__ClientId=cid.apps.googleusercontent.com");
        content.Should().Contain("FRONTEND_HOST_PORT=3100");
        content.Should().Contain("API_HOST_PORT=5100");
        content.Should().Contain("AiProviders__OpenAI__ApiKey=sk-test");
    }
}
