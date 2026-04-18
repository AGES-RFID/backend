namespace Backend.Features.Transactions;


public class Transaction
{
    public Guid UserId { get; set; }
    public decimal Amount { get; set; }
    public DateTime CreatedAt { get; set; }
    public required string Description { get; set; }
    public required TransactionType TransactionType  { get; set; }
}