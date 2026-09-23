using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WalletSystem.Api.Common;
using WalletSystem.Application.Transactions;
using WalletSystem.Domain.Enums;

namespace WalletSystem.Api.Controllers;

/// <summary>
/// The signed-in user's own unified money-movement history - cash-in and
/// expenses today, P2P transfers once that feature exists. Creating a
/// transaction still goes through its own type-specific endpoint
/// (POST /expenses, POST /wallets/{id}/cash-in, later POST /transfers); this
/// only reads. Admins don't have a personal wallet, so this is user-only -
/// an admin viewing another user's transactions is a separate, not yet
/// built, admin endpoint.
/// </summary>
[ApiController]
[Authorize(Roles = nameof(Role.USER))]
[Route("transactions")]
public class TransactionsController : ControllerBase
{
    private readonly ITransactionService _service;

    public TransactionsController(ITransactionService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetMine(CancellationToken ct)
    {
        var userId = User.GetUserId();
        var result = await _service.GetMyTransactionsAsync(userId, ct);
        return StatusCode(result.Status, result);
    }
}
