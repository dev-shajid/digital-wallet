using WalletSystem.Application.Auth.Models;

namespace WalletSystem.Application.Abstractions;

public interface IOtpService
{
    /// <summary>
    /// Stores the user's pending registration data (name, email, hashed password) in
    /// cache, generates a 6-digit OTP, and sends it to the user's email address.
    /// Nothing is written to the database yet.
    /// </summary>
    Task InitiateRegistrationAsync(PendingRegistrationData registrationData);

    /// <summary>
    /// Checks the submitted OTP against the one stored in cache for this email.
    /// Returns Success / InvalidCode / Expired (see OtpValidationResult).
    /// On Success the OTP entry is marked as used to prevent replay attacks.
    /// </summary>
    OtpValidationResult ValidateOtp(string email, string submittedOtp);

    /// <summary>
    /// Retrieves the pending registration data from cache (name, email, hashed password).
    /// Returns null if the entry has expired or never existed.
    /// </summary>
    PendingRegistrationData? GetPendingRegistration(string email);

    /// <summary>
    /// Removes the pending registration entry from cache entirely.
    /// Called after a successful verify-email so the cache stays clean.
    /// </summary>
    void ClearPendingRegistration(string email);

    /// <summary>
    /// Regenerates the OTP for an existing pending registration and resends the email.
    /// Returns false if no pending registration is found for this email
    /// (user must call initiate-registration again).
    /// </summary>
    Task<bool> ResendOtpAsync(string email);
}