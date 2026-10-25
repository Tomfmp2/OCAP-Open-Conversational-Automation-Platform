using FluentAssertions;
using OCAP.Security.Domain.Entities;

namespace OCAP.Security.Tests;

public class UserLockoutTests
{
    private static UserIdentity NewUser() => new(Guid.NewGuid(), Guid.NewGuid(), "ana@ocap.io", "hash", "salt", "Ana");

    [Fact]
    public void FiveFailedLogins_LockTheAccountTemporarily()
    {
        var user = NewUser();

        for (var i = 0; i < 4; i++) user.RegisterFailedLogin(5, TimeSpan.FromMinutes(15));
        user.IsLockedOut(DateTime.UtcNow).Should().BeFalse();

        user.RegisterFailedLogin(5, TimeSpan.FromMinutes(15));
        user.IsLockedOut(DateTime.UtcNow).Should().BeTrue();
        user.IsLockedOut(DateTime.UtcNow.AddMinutes(16)).Should().BeFalse("el bloqueo por intentos es temporal");
    }

    [Fact]
    public void ASuccessfulLogin_ResetsTheCounter()
    {
        var user = NewUser();
        for (var i = 0; i < 4; i++) user.RegisterFailedLogin(5, TimeSpan.FromMinutes(15));

        user.RegisterSuccessfulLogin();
        user.RegisterFailedLogin(5, TimeSpan.FromMinutes(15));

        user.IsLockedOut(DateTime.UtcNow).Should().BeFalse();
        user.FailedLoginCount.Should().Be(1);
    }

    [Fact]
    public void AnAdministrativeLock_DoesNotExpire()
    {
        var user = NewUser();
        user.Lock();

        user.IsLockedOut(DateTime.UtcNow.AddYears(1)).Should().BeTrue();
    }
}
