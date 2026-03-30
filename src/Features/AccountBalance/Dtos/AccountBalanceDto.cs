namespace Backend.Features.AccountBalance.Dtos;

public class AccountBalanceDto
{
    public int CustomerId { get; set; }
    public decimal Balance { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
