using Backend.Features.Transactions;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Features.Transactions;

[ApiController]
[Route("api/transactions")]

public class TransactionsController(ITransactionService transactionService) : ControllerBase
{
    private readonly ITransactionService _transactionService = transactionService;

    [HttpPost]
    public async Task<ActionResult<TransactionResponseDto>> CreateTransaction(CreateTransactionDto dto)
    {
        try
        {
            var transaction = await _transactionService.CreateTransactionAsync(dto);
            return Created(string.Empty, transaction);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
