namespace Backend.Features.Transactions;

public class TransactionResponseDto
{
    public required Guid TransactionId { get; set; }
    public required Guid UserId { get; set; }
    public required decimal Amount { get; set; }
    public required string Description { get; set; }
    public required TransactionType TransactionType { get; set; }
    public required DateTime CreatedAt { get; set; }
}
