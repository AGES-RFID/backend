using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Backend.Features.Transactions;
using Backend.Features.Users;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Features.Transactions;

[ApiController]
[Route("api/transactions")]

public class TransactionsController(ITransactionService transactionService) : ControllerBase
{
    private readonly ITransactionService _transactionService = transactionService;

    [HttpGet("{id}")]
    public async Task<ActionResult<TransactionDto>> GetTransaction(Guid id)
    {
        throw new NotImplementedException();
    }

    [HttpPost]
    public async Task<ActionResult<TransactionDto>> CreateTransaction(CreateTransactionRequestDto dto)
    {
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (!Guid.TryParse(sub, out var actorUserId))
            return Unauthorized();

        var isAdmin = User.IsInRole(UserRole.Admin.ToString());
        var targetUserId = dto.UserId ?? actorUserId;

        var command = new CreateTransactionCommand
        {
            ActorUserId = actorUserId,
            TargetUserId = targetUserId,
            Description = dto.Description,
            Amount = dto.Amount
        };

        try
        {
            var transaction = await _transactionService.CreateTransactionAsync(command);
            return CreatedAtAction(nameof(GetTransaction), new { id = transaction.TransactionId }, transaction);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
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
