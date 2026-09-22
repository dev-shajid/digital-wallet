namespace WalletSystem.Domain.Common;

/// <summary>
/// Base class for entities that have their own primary key and audit timestamps.
/// Think of this like a shared Mongoose schema/Prisma base model: every table that
/// inherits this gets a UUID "id", plus "createdAt"/"updatedAt", for free.
///
/// Not every table uses this - a few "child" tables (BankTransfer, P2PTransfer,
/// Expense) use the parent Transaction's id as their own primary key instead,
/// so they don't inherit from here. See those classes for why.
/// </summary>
public abstract class AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
