using Microsoft.Extensions.Caching.Memory;
using WalletSystem.Application.Abstractions;

namespace WalletSystem.Infrastructure.Services;

public class OtpService : IOtpService
{
    private readonly IMemoryCache _cache;
    private readonly IEmailService _emailService;

    private record OtpCacheEntry(string Code, bool IsUsed);

    public OtpService(IMemoryCache cache, IEmailService emailService)
    {
        _cache = cache;
        _emailService = emailService;
    }

    public async Task GenerateAndSendOtpAsync(string email)
    {
        var otp = Random.Shared.Next(100000, 999999).ToString();
        var cacheEntry = new OtpCacheEntry(otp, IsUsed: false);

        _cache.Set(
            GetCacheKey(email), 
            cacheEntry, 
            TimeSpan.FromMinutes(10));

        await _emailService.SendOtpEmailAsync(email, otp);
    }

    public bool ValidateOtp(string email, string submittedOtp)
    {
        var cacheKey = GetCacheKey(email);

        if (!_cache.TryGetValue(cacheKey, out OtpCacheEntry? entry) || entry == null)
        {
            return false;
        }

        if (entry.IsUsed || entry.Code != submittedOtp)
        {
            return false;
        }

        // Mark as used to prevent replay attacks
        _cache.Set(
            cacheKey, 
            entry with { IsUsed = true }, 
            TimeSpan.FromMinutes(10));

        return true;
    }

    private static string GetCacheKey(string email) => $"otp_{email.ToLower()}";
}