using WalletSystem.Domain.Common;
using WalletSystem.Domain.Enums;

namespace WalletSystem.Domain.Entities;

/// <summary>A registered user. Every user automatically owns one BDT Wallet (created together, in one DB transaction).</summary>
public class User : AuditableEntity
{
    public string Name { get; set; } = string.Empty;

    /// <summary>Unique login email.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>System-generated 10-digit account number (Luhn check digit). Immutable once set.</summary>
    public string AccountNo { get; set; } = string.Empty;

    /// <summary>Hashed password - the plain-text password is never stored.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    public Role Role { get; set; } = Role.USER;

    // Navigation properties: the "other side" of a relationship, like a populated
    // field in Mongoose. EF Core fills these in only when you ask it to (.Include(...)).
    public ICollection<Wallet> Wallets { get; set; } = new List<Wallet>();

    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
