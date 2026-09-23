namespace WalletSystem.Application.Abstractions;

public interface IEmailService
{
    Task SendOtpEmailAsync(string toEmail, string otp);
}