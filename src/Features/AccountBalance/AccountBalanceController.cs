using Microsoft.AspNetCore.Mvc;

namespace Backend.Features.AccountBalance;

[ApiController]
[Route("api/customers/{customerId}/balance")]
public class AccountBalanceController(IAccountBalanceService accountBalanceService) : ControllerBase
{
    private readonly IAccountBalanceService _accountBalanceService = accountBalanceService;

    [HttpGet]
    public async Task<ActionResult<AccountBalanceDto>> GetBalance(int customerId)
    {
        try
        {
            var balance = await _accountBalanceService.GetBalanceAsync(customerId);
            return Ok(balance);
        }
        catch (Exception)
        {
            return BadRequest("Unable to retrieve account balance");
        }
    }

    [HttpGet("transactions")]
    public async Task<ActionResult<IEnumerable<TransactionDto>>> GetTransactions(int customerId)
    {
        try
        {
            var transactions = await _accountBalanceService.GetTransactionsAsync(customerId);
            return Ok(transactions);
        }
        catch (Exception)
        {
            return BadRequest("Unable to retrieve transactions");
        }
    }

    [HttpPost("deposit")]
    public async Task<ActionResult<TransactionDto>> Deposit(int customerId, DepositDto dto)
    {
        try
        {
            var transaction = await _accountBalanceService.DepositAsync(customerId, dto);
            return CreatedAtAction(nameof(GetTransactions), new { customerId }, transaction);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("withdraw")]
    public async Task<ActionResult<TransactionDto>> Withdraw(int customerId, WithdrawDto dto)
    {
        try
        {
            var transaction = await _accountBalanceService.WithdrawAsync(customerId, dto);
            return CreatedAtAction(nameof(GetTransactions), new { customerId }, transaction);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
