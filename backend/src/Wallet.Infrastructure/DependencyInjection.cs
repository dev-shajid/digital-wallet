using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WalletSystem.Infrastructure.Persistence;

namespace WalletSystem.Infrastructure;

/// <summary>
/// A single place to register everything this layer provides. Wallet.Api calls
/// `services.AddInfrastructure(configuration)` once in Program.cs instead of
/// knowing the details of how the DbContext is wired up (similar to an Express app
/// calling one `setupDatabase(app)` helper instead of inlining the Prisma/Mongoose
/// connection logic in the entry file).
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

        return services;
    }
}
