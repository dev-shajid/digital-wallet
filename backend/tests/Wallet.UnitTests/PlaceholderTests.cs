using WalletSystem.Domain.Entities;
using WalletSystem.Domain.Enums;

namespace WalletSystem.UnitTests;

/// <summary>
/// One real test to prove the test project is wired up correctly (references Domain,
/// runs under xUnit). Real unit tests for business rules (balance math, transfer
/// validation, etc.) are added in a later phase once that logic exists.
/// </summary>
public class PlaceholderTests
{
    [Fact]
    public void User_DefaultsToUserRole()
    {
        var user = new User();

        Assert.Equal(Role.USER, user.Role);
    }
}
