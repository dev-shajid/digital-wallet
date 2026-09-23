using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WalletSystem.Api.Common;
using WalletSystem.Application.Transactions;

namespace WalletSystem.Api.Controllers;

/// <summary>
/// The user's unified money-movement history - cash-in and expenses today,
/// P2P transfers once that feature exists. Creating a transaction still goes
/// through its own type-specific endpoint (POST /expenses, POST /wallets/{id}/cash-in,
/// later POST /transfers); this only reads.
/// </summary>
[ApiController]
[Authorize]
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
