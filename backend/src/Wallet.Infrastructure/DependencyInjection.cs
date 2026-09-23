using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WalletSystem.Application.Abstractions;
using WalletSystem.Application.Expenses;
using WalletSystem.Infrastructure.Persistence;
using WalletSystem.Infrastructure.Services;
using WalletSystem.Infrastructure.Settings;

namespace WalletSystem.Infrastructure;

/// <summary>
/// A single place to register everything this layer provides.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Missing 'ConnectionStrings:Default' configuration value.");

        services.AddDbContext<WalletDbContext>(options =>
            options.UseNpgsql(connectionString)
                // Converts C# PascalCase (CreatedAt) to Postgres snake_case (created_at)
                // for every table and column automatically.
                .UseSnakeCaseNamingConvention());

        // Bind SMTP settings from appsettings.json
        services.Configure<SmtpSettings>(configuration.GetSection("Smtp"));

        // Register built-in In-Memory Cache
        services.AddMemoryCache();

        // Register Email and OTP Services
        services.AddTransient<IEmailService, EmailService>();
        services.AddScoped<IOtpService, OtpService>();

        // Authentication & Security Services
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IAccountNumberGenerator, AccountNumberGenerator>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddSingleton<IRefreshTokenGenerator, RefreshTokenGenerator>();
        services.AddScoped<IExpenseCategoryService, ExpenseCategoryService>();
        services.AddScoped<IExpenseService, ExpenseService>();

        return services;
    }
}
