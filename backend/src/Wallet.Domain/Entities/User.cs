using Wallet.Domain.Common;
using Wallet.Domain.Enums;

namespace Wallet.Domain.Entities;

public class User : AuditableEntity
{
    public required string Name { get; set; }

    public required string Email { get; set; }

    public required string AccountNo { get; set; }

    public required string PasswordHash { get; set; }

    public Role Role { get; set; }

    public ICollection<Wallet> Wallets { get; set; } = [];
}
