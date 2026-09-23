using Microsoft.Extensions.Caching.Memory;
using WalletSystem.Application.Abstractions;
using WalletSystem.Application.Auth.Models;

namespace WalletSystem.Infrastructure.Services;

public class OtpService : IOtpService
{
    private readonly IMemoryCache _cache;
    private readonly IEmailService _emailService;

    // The cache entry holds both the OTP info AND the pending registration data
    // so we never need to create the user in the DB before the OTP is verified.
    private record CacheEntry(
        string OtpCode,
        bool IsUsed,
        PendingRegistrationData Registration
    );

    private static readonly TimeSpan OtpLifetime = TimeSpan.FromMinutes(10);

    public OtpService(IMemoryCache cache, IEmailService emailService)
    {
        _cache = cache;
        _emailService = emailService;
    }

    /// <inheritdoc/>
    public async Task InitiateRegistrationAsync(PendingRegistrationData registrationData)
    {
        var otp = Random.Shared.Next(100000, 999999).ToString();

        var entry = new CacheEntry(
            OtpCode: otp,
            IsUsed: false,
            Registration: registrationData
        );

        // Any previous pending registration for the same email is overwritten.
        _cache.Set(GetCacheKey(registrationData.Email), entry, OtpLifetime);

        await _emailService.SendOtpEmailAsync(registrationData.Email, otp);
    }

    /// <inheritdoc/>
    public OtpValidationResult ValidateOtp(string email, string submittedOtp)
    {
        var cacheKey = GetCacheKey(email);

        // TryGetValue returns false when the entry doesn't exist OR has expired.
        if (!_cache.TryGetValue(cacheKey, out CacheEntry? entry) || entry is null)
        {
            return OtpValidationResult.Expired;
        }

        // Already consumed (replay protection).
        if (entry.IsUsed)
        {
            return OtpValidationResult.Expired;
        }

        if (entry.OtpCode != submittedOtp)
        {
            return OtpValidationResult.InvalidCode;
        }

        // Mark as used to prevent the same OTP from being accepted twice.
        // Keep it in cache a bit longer so GetPendingRegistration can still read the
        // registration data when the controller calls it right after this validation.
        _cache.Set(cacheKey, entry with { IsUsed = true }, OtpLifetime);

        return OtpValidationResult.Success;
    }

    /// <inheritdoc/>
    public PendingRegistrationData? GetPendingRegistration(string email)
    {
        if (_cache.TryGetValue(GetCacheKey(email), out CacheEntry? entry) && entry is not null)
        {
            return entry.Registration;
        }

        return null;
    }

    /// <inheritdoc/>
    public void ClearPendingRegistration(string email)
    {
        _cache.Remove(GetCacheKey(email));
    }

    /// <inheritdoc/>
    public async Task<bool> ResendOtpAsync(string email)
    {
        // Only resend if a pending registration actually exists.
        // If the cache expired the user must start over from initiate-registration.
        if (!_cache.TryGetValue(GetCacheKey(email), out CacheEntry? existing) || existing is null)
        {
            return false;
        }

        // Generate a fresh OTP and restart the 10-minute window.
        var newOtp = Random.Shared.Next(100000, 999999).ToString();
        var newEntry = new CacheEntry(
            OtpCode: newOtp,
            IsUsed: false,
            Registration: existing.Registration   // keep the original registration data
        );

        _cache.Set(GetCacheKey(email), newEntry, OtpLifetime);

        await _emailService.SendOtpEmailAsync(email, newOtp);

        return true;
    }

    // Separate key namespace from any other cache usage in the app.
    private static string GetCacheKey(string email) => $"pending_reg_{email.ToLowerInvariant()}";
}