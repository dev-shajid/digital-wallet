namespace Wallet.Application.Features.Auth;

public record UserDto(
    Guid Id,
    string Name,
    string Email,
    string AccountNo,
    string Role,
    DateTime CreatedAt);

public record UserWalletDto(
    Guid Id,
    Guid CurrencyId,
    string CurrencyCode,
    string CurrencySymbol,
    decimal Balance,
    string Status);

public record UserProfileDto(
    Guid Id,
    string Name,
    string Email,
    string AccountNo,
    string Role,
    DateTime CreatedAt,
    IReadOnlyList<UserWalletDto> Wallets);

public record AuthResponse(
    string Token,
    UserDto User);
