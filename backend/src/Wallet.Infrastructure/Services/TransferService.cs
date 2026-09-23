using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

using WalletSystem.Application.Abstractions;
using WalletSystem.Application.Common.Models;
using WalletSystem.Application.Transfers.Models;
using WalletSystem.Domain.Entities;
using WalletSystem.Domain.Enums;
using WalletSystem.Infrastructure.Persistence;

namespace WalletSystem.Infrastructure.Services;

public sealed class TransferService : ITransferService
{
    private readonly WalletDbContext _dbContext;
    private readonly IAccountNumberGenerator _accountNumberGenerator;

    public TransferService(
        WalletDbContext dbContext,
        IAccountNumberGenerator accountNumberGenerator)
    {
        _dbContext = dbContext;
        _accountNumberGenerator = accountNumberGenerator;
    }

    public async Task<ApiResponse<TransferResponse>> TransferAsync(
        Guid senderUserId,
        TransferRequest request,
        CancellationToken ct = default)
    {
        if (senderUserId == Guid.Empty)
        {
            return Failure<TransferResponse>(
                StatusCodes.Status401Unauthorized,
                "Invalid sender identity.");
        }

        if (request is null)
        {
            return Failure<TransferResponse>(
                StatusCodes.Status400BadRequest,
                "Transfer request is required.");
        }

        if (string.IsNullOrWhiteSpace(request.ReceiverAccountNo))
        {
            return Failure<TransferResponse>(
                StatusCodes.Status400BadRequest,
                "Receiver account number is required.");
        }

        string receiverAccountNo = request.ReceiverAccountNo
            .Trim()
            .ToUpperInvariant();

        if (!_accountNumberGenerator.ValidateAccountNumber(receiverAccountNo))
        {
            return Failure<TransferResponse>(
                StatusCodes.Status400BadRequest,
                "Invalid receiver account number.");
        }

        if (request.CurrencyId == Guid.Empty)
        {
            return Failure<TransferResponse>(
                StatusCodes.Status400BadRequest,
                "Currency ID is required.");
        }

        if (request.Amount <= 0)
        {
            return Failure<TransferResponse>(
                StatusCodes.Status400BadRequest,
                "Transfer amount must be greater than zero.");
        }

        if (request.Amount != decimal.Round(request.Amount, 4))
        {
            return Failure<TransferResponse>(
                StatusCodes.Status400BadRequest,
                "Transfer amount cannot have more than 4 decimal places.");
        }

        if (request.Note is not null && request.Note.Length > 255)
        {
            return Failure<TransferResponse>(
                StatusCodes.Status400BadRequest,
                "Note cannot exceed 255 characters.");
        }

        var currency = await _dbContext.Currencies
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.Id == request.CurrencyId &&
                        item.Status == CurrencyStatus.ACTIVE,
                ct);

        if (currency is null)
        {
            return Failure<TransferResponse>(
                StatusCodes.Status400BadRequest,
                "The selected currency is invalid or inactive.");
        }

        var receiverUser = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(
                user => user.AccountNo == receiverAccountNo,
                ct);

        if (receiverUser is null)
        {
            return Failure<TransferResponse>(
                StatusCodes.Status404NotFound,
                "Receiver account was not found.");
        }

        if (receiverUser.Id == senderUserId)
        {
            return Failure<TransferResponse>(
                StatusCodes.Status400BadRequest,
                "You cannot transfer money to your own account.");
        }

        await using var databaseTransaction =
            await _dbContext.Database.BeginTransactionAsync(ct);

        var senderWalletId = await _dbContext.Wallets
            .Where(wallet =>
                wallet.UserId == senderUserId &&
                wallet.CurrencyId == request.CurrencyId)
            .Select(wallet => (Guid?)wallet.Id)
            .SingleOrDefaultAsync(ct);

        if (senderWalletId is null)
        {
            return Failure<TransferResponse>(
                StatusCodes.Status404NotFound,
                "Sender does not have a wallet for the selected currency.");
        }

        var receiverWalletId = await _dbContext.Wallets
            .Where(wallet =>
                wallet.UserId == receiverUser.Id &&
                wallet.CurrencyId == request.CurrencyId)
            .Select(wallet => (Guid?)wallet.Id)
            .SingleOrDefaultAsync(ct);

        if (receiverWalletId is null)
        {
            return Failure<TransferResponse>(
                StatusCodes.Status404NotFound,
                "Receiver does not have a wallet for the selected currency.");
        }

        Wallet senderWallet;
        Wallet receiverWallet;

        // Lock wallets in a deterministic order to reduce deadlock risk.
        if (senderWalletId.Value.CompareTo(receiverWalletId.Value) < 0)
        {
            senderWallet = await LoadWalletForUpdateAsync(
                senderWalletId.Value,
                ct);

            receiverWallet = await LoadWalletForUpdateAsync(
                receiverWalletId.Value,
                ct);
        }
        else
        {
            receiverWallet = await LoadWalletForUpdateAsync(
                receiverWalletId.Value,
                ct);

            senderWallet = await LoadWalletForUpdateAsync(
                senderWalletId.Value,
                ct);
        }

        if (senderWallet.UserId != senderUserId)
        {
            return Failure<TransferResponse>(
                StatusCodes.Status403Forbidden,
                "Sender wallet does not belong to the authenticated user.");
        }

        if (receiverWallet.UserId != receiverUser.Id)
        {
            return Failure<TransferResponse>(
                StatusCodes.Status409Conflict,
                "Receiver wallet ownership could not be verified.");
        }

        if (senderWallet.CurrencyId != request.CurrencyId ||
            receiverWallet.CurrencyId != request.CurrencyId)
        {
            return Failure<TransferResponse>(
                StatusCodes.Status409Conflict,
                "Wallet currency does not match the requested currency.");
        }

        if (senderWallet.CurrencyId != receiverWallet.CurrencyId)
        {
            return Failure<TransferResponse>(
                StatusCodes.Status422UnprocessableEntity,
                "Sender and receiver wallets must use the same currency.");
        }

        if (senderWallet.Status != WalletStatus.ACTIVE)
        {
            return Failure<TransferResponse>(
                StatusCodes.Status409Conflict,
                "Sender wallet is not active.");
        }

        if (receiverWallet.Status != WalletStatus.ACTIVE)
        {
            return Failure<TransferResponse>(
                StatusCodes.Status409Conflict,
                "Receiver wallet is not active.");
        }

        if (senderWallet.Balance < request.Amount)
        {
            return Failure<TransferResponse>(
                StatusCodes.Status422UnprocessableEntity,
                "Insufficient wallet balance.");
        }

        decimal senderBalanceBefore = senderWallet.Balance;
        decimal receiverBalanceBefore = receiverWallet.Balance;

        decimal senderBalanceAfter =
            senderBalanceBefore - request.Amount;

        decimal receiverBalanceAfter =
            receiverBalanceBefore + request.Amount;

        var transaction = new Transaction
        {
            UserId = senderUserId,
            CurrencyId = request.CurrencyId,
            Type = TransactionType.P2P_TRANSFER,
            Amount = request.Amount,
            Status = TransactionStatus.SUCCESS,
            Reference = $"P2P-{Guid.NewGuid():N}".ToUpperInvariant(),
            Note = string.IsNullOrWhiteSpace(request.Note)
                ? null
                : request.Note.Trim()
        };

        var p2pTransfer = new P2PTransfer
        {
            Transaction = transaction,
            ReceiverWalletId = receiverWallet.Id
        };

        var senderWalletLog = new WalletLog
        {
            Wallet = senderWallet,
            Transaction = transaction,
            Direction = WalletLogDirection.DEBIT,
            Amount = request.Amount,
            BalanceBefore = senderBalanceBefore,
            BalanceAfter = senderBalanceAfter
        };

        var receiverWalletLog = new WalletLog
        {
            Wallet = receiverWallet,
            Transaction = transaction,
            Direction = WalletLogDirection.CREDIT,
            Amount = request.Amount,
            BalanceBefore = receiverBalanceBefore,
            BalanceAfter = receiverBalanceAfter
        };

        senderWallet.Balance = senderBalanceAfter;
        senderWallet.UpdatedAt = DateTime.UtcNow;

        receiverWallet.Balance = receiverBalanceAfter;
        receiverWallet.UpdatedAt = DateTime.UtcNow;

        await _dbContext.Transactions.AddAsync(transaction, ct);
        await _dbContext.P2PTransfers.AddAsync(p2pTransfer, ct);
        await _dbContext.WalletLogs.AddRangeAsync(
            [senderWalletLog, receiverWalletLog],
            ct);

        await _dbContext.SaveChangesAsync(ct);
        await databaseTransaction.CommitAsync(ct);

        return new ApiResponse<TransferResponse>
        {
            Success = true,
            Status = StatusCodes.Status201Created,
            Message = "Transfer completed successfully.",
            Data = new TransferResponse
            {
                TransactionId = transaction.Id,
                Reference = transaction.Reference,

                CurrencyId = currency.Id,
                CurrencyCode = currency.Code,

                ReceiverAccountNo = receiverUser.AccountNo,
                ReceiverName = receiverUser.Name,
                Amount = request.Amount,
                SenderBalanceAfter = senderBalanceAfter,
                Status = transaction.Status.ToString(),
                CreatedAt = transaction.CreatedAt
            }
        };
    }

    public async Task<ApiResponse<List<TransferHistoryItem>>> GetHistoryAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        if (userId == Guid.Empty)
        {
            return new ApiResponse<List<TransferHistoryItem>>
            {
                Success = false,
                Status = StatusCodes.Status401Unauthorized,
                Message = "Invalid user identity.",
                Data = null
            };
        }

        var logs = await _dbContext.WalletLogs
            .AsNoTracking()
            .Include(log => log.Wallet)
                .ThenInclude(wallet => wallet.User)
            .Include(log => log.Transaction)
                .ThenInclude(transaction => transaction.User)
            .Include(log => log.Transaction)
                .ThenInclude(transaction => transaction.P2PTransfer)
                    .ThenInclude(transfer => transfer!.ReceiverWallet)
                        .ThenInclude(wallet => wallet.User)
            .Where(log =>
                log.Wallet.UserId == userId &&
                log.Transaction.Type == TransactionType.P2P_TRANSFER)
            .OrderByDescending(log => log.CreatedAt)
            .ToListAsync(ct);

        var history = logs
            .Select(log =>
            {
                bool isSent = log.Direction == WalletLogDirection.DEBIT;

                string counterpartyAccountNo;
                string counterpartyName;

                if (isSent)
                {
                    counterpartyAccountNo =
                        log.Transaction.P2PTransfer!
                            .ReceiverWallet.User.AccountNo;

                    counterpartyName =
                        log.Transaction.P2PTransfer!
                            .ReceiverWallet.User.Name;
                }
                else
                {
                    counterpartyAccountNo =
                        log.Transaction.User.AccountNo;

                    counterpartyName =
                        log.Transaction.User.Name;
                }

                return new TransferHistoryItem
                {
                    TransactionId = log.TransactionId,
                    Reference = log.Transaction.Reference,
                    Direction = isSent ? "SENT" : "RECEIVED",
                    CounterpartyAccountNo = counterpartyAccountNo,
                    CounterpartyName = counterpartyName,
                    Amount = log.Amount,
                    Status = log.Transaction.Status.ToString(),
                    Note = log.Transaction.Note,
                    CreatedAt = log.Transaction.CreatedAt
                };
            })
            .ToList();

        return new ApiResponse<List<TransferHistoryItem>>
        {
            Success = true,
            Status = StatusCodes.Status200OK,
            Message = "Transfer history retrieved successfully.",
            Data = history
        };
    }

    private async Task<Wallet> LoadWalletForUpdateAsync(
        Guid walletId,
        CancellationToken ct)
    {
        return await _dbContext.Wallets
            .FromSqlInterpolated($"""
                SELECT *
                FROM wallets
                WHERE id = {walletId}
                FOR UPDATE
                """)
            .Include(wallet => wallet.Currency)
            .SingleAsync(ct);
    }

    private static ApiResponse<T> Failure<T>(
        int status,
        string message)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Status = status,
            Message = message,
            Data = default
        };
    }
}