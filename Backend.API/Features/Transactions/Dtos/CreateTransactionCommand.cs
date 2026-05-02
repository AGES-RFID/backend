namespace Backend.Features.Transactions;

public class CreateTransactionCommand
{
    public required Guid ActorUserId { get; init; }
    public required Guid TargetUserId { get; init; }
    public required string Description { get; init; }
    public required decimal Amount { get; init; }
}
