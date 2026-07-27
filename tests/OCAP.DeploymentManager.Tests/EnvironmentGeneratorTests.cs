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
            PostgresDbName = "custom_ocap_db"
        };

        // Act
        var content = _generator.GenerateEnvironmentFileContent(config);

        // Assert
        content.Should().Contain("POSTGRES_HOST=db.example.com");
        content.Should().Contain("POSTGRES_DB=custom_ocap_db");
        content.Should().Contain("JWT_SECRET_KEY=");
    }
}
