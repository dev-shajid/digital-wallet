using Wallet.Application.Common.Models.Bank;

namespace Wallet.Application.Common.Interfaces;

public interface IBankClient
{
    Task<BankDebitResult> DebitAsync(BankDebitRequest request, CancellationToken ct = default);
}
