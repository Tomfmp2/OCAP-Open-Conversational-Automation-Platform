using FluentAssertions;
using OCAP.DeploymentManager.Models;
using OCAP.DeploymentManager.Services;

namespace OCAP.DeploymentManager.Tests;

public class DeploymentValidatorTests
{
    private readonly DeploymentValidator _validator = new();

    [Fact]
    public void Validate_WithValidConfiguration_ReturnsSuccess()
    {
        // Arrange
        var config = new DeploymentConfiguration();

        // Act
        var (isValid, errors) = _validator.Validate(config);

        // Assert
        isValid.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithEmptyPostgresPassword_ReturnsErrors()
    {
        // Arrange
        var config = new DeploymentConfiguration
        {
            PostgresPassword = ""
        };

        // Act
        var (isValid, errors) = _validator.Validate(config);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain(e => e.Contains("contraseña"));
    }
}
