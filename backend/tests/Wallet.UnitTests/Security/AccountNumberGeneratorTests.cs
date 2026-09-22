using NSubstitute;
using Wallet.Application.Common.Interfaces;
using Wallet.Infrastructure.Security;

namespace Wallet.UnitTests.Security;

public class AccountNumberGeneratorTests
{
    private readonly AccountNumberGenerator _generator = new();

    [Theory]
    [InlineData(100000001L, "AC1000000016")]
    [InlineData(100000002L, "AC1000000024")]
    [InlineData(100000003L, "AC1000000032")]
    [InlineData(100000004L, "AC1000000040")]
    [InlineData(100000005L, "AC1000000057")]
    public void FormatFromSequence_ShouldProduceExpectedAccountNumbers(long sequence, string expected)
    {
        var result = _generator.FormatFromSequence(sequence);

        Assert.Equal(expected, result);
        Assert.Equal(12, result.Length);
        Assert.StartsWith("AC", result);
        Assert.True(_generator.Validate(result));
    }

    [Fact]
    public void FormatFromSequence_ShouldPassLuhnValidationForRange()
    {
        for (var seq = 100000001L; seq <= 100000100L; seq++)
        {
            var accountNo = _generator.FormatFromSequence(seq);
            Assert.True(_generator.Validate(accountNo), $"Account number {accountNo} for sequence {seq} failed validation.");
        }
    }

    [Fact]
    public async Task GenerateAsync_ShouldCallDbContextSequenceAndFormat()
    {
        var dbContext = Substitute.For<IAppDbContext>();
        dbContext.NextAccountSequenceValueAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(100000001L));

        var generator = new AccountNumberGenerator(dbContext);
        var accountNo = await generator.GenerateAsync();

        Assert.Equal("AC1000000016", accountNo);
        Assert.True(generator.Validate(accountNo));
    }

    [Theory]
    [InlineData("123456789012")]
    [InlineData("AC12345")]
    [InlineData("")]
    [InlineData("ACabcdefghij")]
    [InlineData("WL1234567890")]
    [InlineData("AC1000000019")] // Wrong Luhn check digit (expected 6)
    public void Validate_InvalidInput_ShouldReturnFalse(string accountNo)
    {
        var isValid = _generator.Validate(accountNo);
        Assert.False(isValid);
    }
}
