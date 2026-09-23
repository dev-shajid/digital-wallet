namespace WalletSystem.Application.Abstractions;

public interface IOtpService
{
    Task GenerateAndSendOtpAsync(string email);
    bool ValidateOtp(string email, string submittedOtp);
}