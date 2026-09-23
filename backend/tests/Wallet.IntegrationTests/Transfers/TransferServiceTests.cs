using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using WalletSystem.Application.Transfers.Models;
using WalletSystem.Domain.Entities;
using WalletSystem.Domain.Enums;
using WalletSystem.Infrastructure.Persistence;
using WalletSystem.Infrastructure.Persistence.Seed;
using WalletSystem.Infrastructure.Services;
using Xunit;

namespace WalletSystem.IntegrationTests.Transfers;

public class TransferServiceTests
{
    private static WalletDbContext CreateInMemoryDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<WalletDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        var db = new WalletDbContext(options);

        // Seed Currency
        if (!db.Currencies.Any(c => c.Id == CurrencySeed.BdtId))
        {
            db.Currencies.Add(new Currency
            {
                Id = CurrencySeed.BdtId,
                Code = "BDT",
                Name = "Bangladeshi Taka",
                Symbol = "৳",
                CountryCode = "BD",
                DecimalPlaces = 2,
                Status = CurrencyStatus.ACTIVE
            });
            db.SaveChanges();
        }

        return db;
    }

    [Fact]
    public async Task TransferAsync_ValidRequest_CreditsReceiverDebitsSenderAndWritesLogs()
    {
        // Arrange
        var db = CreateInMemoryDbContext(nameof(TransferAsync_ValidRequest_CreditsReceiverDebitsSenderAndWritesLogs));
        var service = new TransferService(db);

        var sender = new User
        {
            Id = Guid.NewGuid(),
            Name = "Alice",
            Email = "alice@example.com",
            AccountNo = "AC00000001",
            PasswordHash = "hash",
            Role = Role.USER
        };

        var receiver = new User
        {
            Id = Guid.NewGuid(),
            Name = "Bob",
            Email = "bob@example.com",
            AccountNo = "AC00000002",
            PasswordHash = "hash",
            Role = Role.USER
        };

        var senderWallet = new Wallet
        {
            Id = Guid.NewGuid(),
            UserId = sender.Id,
            CurrencyId = CurrencySeed.BdtId,
            Balance = 1000m,
            Status = WalletStatus.ACTIVE
        };

        var receiverWallet = new Wallet
        {
            Id = Guid.NewGuid(),
            UserId = receiver.Id,
            CurrencyId = CurrencySeed.BdtId,
            Balance = 200m,
            Status = WalletStatus.ACTIVE
        };

        await db.Users.AddRangeAsync(sender, receiver);
        await db.Wallets.AddRangeAsync(senderWallet, receiverWallet);
        await db.SaveChangesAsync();

        var request = new TransferRequest
        {
            ReceiverAccountNo = "AC00000002",
            Amount = 300m,
            Note = "Lunch split"
        };

        // Act
        var result = await service.TransferAsync(sender.Id, request);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(StatusCodes.Status201Created, result.Status);
        Assert.NotNull(result.Data);
        Assert.Equal(700m, result.Data.SenderBalanceAfter);
        Assert.Equal("Bob", result.Data.ReceiverName);

        // Verify DB updates
        var updatedSenderWallet = await db.Wallets.FindAsync(senderWallet.Id);
        var updatedReceiverWallet = await db.Wallets.FindAsync(receiverWallet.Id);
        Assert.Equal(700m, updatedSenderWallet!.Balance);
        Assert.Equal(500m, updatedReceiverWallet!.Balance);

        // Verify 2 WalletLogs written
        var logs = await db.WalletLogs.Where(l => l.TransactionId == result.Data.TransactionId).ToListAsync();
        Assert.Equal(2, logs.Count);
        Assert.Contains(logs, l => l.Direction == WalletLogDirection.DEBIT && l.Amount == 300m && l.BalanceBefore == 1000m && l.BalanceAfter == 700m);
        Assert.Contains(logs, l => l.Direction == WalletLogDirection.CREDIT && l.Amount == 300m && l.BalanceBefore == 200m && l.BalanceAfter == 500m);

        // Verify P2PTransfer record
        var p2p = await db.P2PTransfers.FindAsync(result.Data.TransactionId);
        Assert.NotNull(p2p);
        Assert.Equal(receiverWallet.Id, p2p.ReceiverWalletId);
    }

    [Fact]
    public async Task TransferAsync_ReceiverNotFound_Returns404()
    {
        // Arrange
        var db = CreateInMemoryDbContext(nameof(TransferAsync_ReceiverNotFound_Returns404));
        var service = new TransferService(db);

        var sender = new User
        {
            Id = Guid.NewGuid(),
            Name = "Alice",
            Email = "alice@example.com",
            AccountNo = "AC00000001",
            PasswordHash = "hash",
            Role = Role.USER
        };

        var senderWallet = new Wallet
        {
            Id = Guid.NewGuid(),
            UserId = sender.Id,
            CurrencyId = CurrencySeed.BdtId,
            Balance = 500m,
            Status = WalletStatus.ACTIVE
        };

        await db.Users.AddAsync(sender);
        await db.Wallets.AddAsync(senderWallet);
        await db.SaveChangesAsync();

        var request = new TransferRequest
        {
            ReceiverAccountNo = "AC99999999",
            Amount = 100m
        };

        // Act
        var result = await service.TransferAsync(sender.Id, request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(StatusCodes.Status404NotFound, result.Status);
    }

    [Fact]
    public async Task TransferAsync_TransferToSelf_Returns400()
    {
        // Arrange
        var db = CreateInMemoryDbContext(nameof(TransferAsync_TransferToSelf_Returns400));
        var service = new TransferService(db);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "Alice",
            Email = "alice@example.com",
            AccountNo = "AC00000001",
            PasswordHash = "hash",
            Role = Role.USER
        };

        var wallet = new Wallet
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            CurrencyId = CurrencySeed.BdtId,
            Balance = 500m,
            Status = WalletStatus.ACTIVE
        };

        await db.Users.AddAsync(user);
        await db.Wallets.AddAsync(wallet);
        await db.SaveChangesAsync();

        var request = new TransferRequest
        {
            ReceiverAccountNo = "AC00000001",
            Amount = 50m
        };

        // Act
        var result = await service.TransferAsync(user.Id, request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(StatusCodes.Status400BadRequest, result.Status);
    }

    [Fact]
    public async Task TransferAsync_InsufficientBalance_Returns422()
    {
        // Arrange
        var db = CreateInMemoryDbContext(nameof(TransferAsync_InsufficientBalance_Returns422));
        var service = new TransferService(db);

        var sender = new User { Id = Guid.NewGuid(), Name = "A", Email = "a@a.com", AccountNo = "AC00000001", PasswordHash = "h" };
        var receiver = new User { Id = Guid.NewGuid(), Name = "B", Email = "b@b.com", AccountNo = "AC00000002", PasswordHash = "h" };

        var senderWallet = new Wallet { Id = Guid.NewGuid(), UserId = sender.Id, CurrencyId = CurrencySeed.BdtId, Balance = 50m, Status = WalletStatus.ACTIVE };
        var receiverWallet = new Wallet { Id = Guid.NewGuid(), UserId = receiver.Id, CurrencyId = CurrencySeed.BdtId, Balance = 0m, Status = WalletStatus.ACTIVE };

        await db.Users.AddRangeAsync(sender, receiver);
        await db.Wallets.AddRangeAsync(senderWallet, receiverWallet);
        await db.SaveChangesAsync();

        var request = new TransferRequest { ReceiverAccountNo = "AC00000002", Amount = 100m };

        // Act
        var result = await service.TransferAsync(sender.Id, request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(StatusCodes.Status422UnprocessableEntity, result.Status);
    }

    [Fact]
    public async Task TransferAsync_FrozenSenderWallet_Returns409()
    {
        // Arrange
        var db = CreateInMemoryDbContext(nameof(TransferAsync_FrozenSenderWallet_Returns409));
        var service = new TransferService(db);

        var sender = new User { Id = Guid.NewGuid(), Name = "A", Email = "a@a.com", AccountNo = "AC00000001", PasswordHash = "h" };
        var receiver = new User { Id = Guid.NewGuid(), Name = "B", Email = "b@b.com", AccountNo = "AC00000002", PasswordHash = "h" };

        var senderWallet = new Wallet { Id = Guid.NewGuid(), UserId = sender.Id, CurrencyId = CurrencySeed.BdtId, Balance = 500m, Status = WalletStatus.FROZEN };
        var receiverWallet = new Wallet { Id = Guid.NewGuid(), UserId = receiver.Id, CurrencyId = CurrencySeed.BdtId, Balance = 0m, Status = WalletStatus.ACTIVE };

        await db.Users.AddRangeAsync(sender, receiver);
        await db.Wallets.AddRangeAsync(senderWallet, receiverWallet);
        await db.SaveChangesAsync();

        var request = new TransferRequest { ReceiverAccountNo = "AC00000002", Amount = 100m };

        // Act
        var result = await service.TransferAsync(sender.Id, request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(StatusCodes.Status409Conflict, result.Status);
    }
}
