namespace Backend.Features.AccountBalance.Models;

public class AccountBalance
{
    public int AccountBalanceId { get; set; }
    public int CustomerId { get; set; }
    public decimal Balance { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
