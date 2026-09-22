using Wallet.Infrastructure.Security;

namespace Wallet.UnitTests.Security;

public class AccountNumberGeneratorTests
{
    private readonly AccountNumberGenerator _generator = new();

    [Fact]
    public void Generate_ShouldReturn12CharactersWithAcPrefix()
    {
        var accountNo = _generator.Generate();

        Assert.NotNull(accountNo);
        Assert.Equal(12, accountNo.Length);
        Assert.StartsWith("AC", accountNo);
        Assert.True(accountNo[2..].All(char.IsDigit));
        Assert.NotEqual('0', accountNo[2]);
    }

    [Fact]
    public void Generate_ShouldPassLuhnValidation()
    {
        for (var i = 0; i < 50; i++)
        {
            var accountNo = _generator.Generate();
            Assert.True(_generator.Validate(accountNo), $"Account number {accountNo} failed validation.");
        }
    }

    [Theory]
    [InlineData("123456789012")]
    [InlineData("AC12345")]
    [InlineData("")]
    [InlineData("ACabcdefghij")]
    [InlineData("WL1234567890")]
    public void Validate_InvalidInput_ShouldReturnFalse(string accountNo)
    {
        var isValid = _generator.Validate(accountNo);
        Assert.False(isValid);
    }
}
