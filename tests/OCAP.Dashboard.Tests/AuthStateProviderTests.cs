using FluentAssertions;
using OCAP.Dashboard.Authentication;

namespace OCAP.Dashboard.Tests;

public class AuthStateProviderTests
{
    [Fact]
    public async Task GetAuthenticationStateAsync_ReturnsAdministratorClaims()
    {
        // Arrange
        var provider = new CustomAuthenticationStateProvider();

        // Act
        var state = await provider.GetAuthenticationStateAsync();

        // Assert
        state.User.Should().NotBeNull();
        state.User.Identity?.Name.Should().Be("Administrador OCAP");
        state.User.IsInRole("Administrator").Should().BeTrue();
    }
}
