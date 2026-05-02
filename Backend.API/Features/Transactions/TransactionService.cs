using Backend.Database;
using Backend.Features.Users;
using Backend.Features.Transactions;
using Microsoft.EntityFrameworkCore;

namespace Backend.Features.Transactions;

public interface ITransactionService
{
    Task<TransactionDto> CreateTransactionAsync(CreateTransactionCommand command);
}

public class TransactionService(AppDbContext db, IUserService userService) : ITransactionService
{
    private readonly AppDbContext _db = db;
    private readonly IUserService _userService = userService;

    public async Task<TransactionDto> CreateTransactionAsync(CreateTransactionCommand command)
    {
        var actor = await _userService.GetUserAsync(command.ActorUserId);

        if (!(actor.Role == UserRole.Admin) && command.ActorUserId != command.TargetUserId)
            throw new UnauthorizedAccessException("Usuário não autorizado");

        var target = await _userService.GetUserAsync(command.TargetUserId)
            ?? throw new InvalidOperationException("Usuário não encontrado");

        var transaction = await _db.Transactions.AddAsync(new Transaction
        {
            UserId = target.UserId,
            Description = command.Description,
            Amount = command.Amount,
            TransactionType = TransactionType.DEPOSIT,
        });

        await _db.SaveChangesAsync();

        return TransactionDto.FromModel(transaction.Entity);
    }
}
