namespace WalletSystem.Application.Auth.Models;

/// <summary>
/// Temporarily stored in the memory cache (keyed by email) while a user's OTP is
/// pending verification. Holds everything needed to create the User + Wallet once
/// the OTP is confirmed. Deleted from cache as soon as verification succeeds or the
/// OTP expires.
/// </summary>
public record PendingRegistrationData(
    string Name,
    string Email,
    string PasswordHash  // already hashed before being stored — plain text never cached
);
