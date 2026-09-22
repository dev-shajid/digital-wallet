using Wallet.Domain.Entities;

namespace Wallet.Application.Common.Interfaces;

public interface ITokenService
{
    string GenerateToken(User user);
}
