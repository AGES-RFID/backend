namespace Backend.Features.Transactions;

public class TransactionDto
{
    public required Guid TransactionId { get; set; }
    public required Guid UserId { get; set; }
    public required decimal Amount { get; set; }
    public required string Description { get; set; }
    public required TransactionType TransactionType { get; set; }
    public required DateTime CreatedAt { get; set; }

    static public TransactionDto FromModel(Transaction transaction) => new()
    {
        TransactionId = transaction.TransactionId,
        UserId = transaction.UserId,
        Amount = transaction.Amount,
        Description = transaction.Description,
        TransactionType = transaction.TransactionType,
        CreatedAt = transaction.CreatedAt
    };
}
