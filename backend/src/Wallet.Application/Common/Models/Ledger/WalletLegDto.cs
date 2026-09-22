using Wallet.Domain.Enums;

namespace Wallet.Application.Common.Models.Ledger;

public record WalletLegDto(Guid WalletId, WalletLogDirection Direction, decimal Amount);
