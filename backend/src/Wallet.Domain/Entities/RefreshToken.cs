using WalletSystem.Domain.Common;

namespace WalletSystem.Domain.Entities;

/// <summary>
/// A long-lived, opaque token issued alongside a short-lived JWT access token, used to obtain
/// a new access token without asking the user to log in again. Only the SHA-256 hash of the
/// token is stored (like a password) so a DB leak alone doesn't hand out usable tokens.
///
/// Rotated on every use: redeeming a refresh token immediately revokes it and issues a new
/// one (<see cref="ReplacedByTokenHash"/> links old to new). If a revoked token is presented
/// again, that's a signal it was stolen and replayed, so the whole family is revoked - see
/// AuthService.RefreshTokenAsync.
/// </summary>
public class RefreshToken : AuditableEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string TokenHash { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    public string? ReplacedByTokenHash { get; set; }

    public bool IsActive => RevokedAt is null && ExpiresAt > DateTime.UtcNow;
}
