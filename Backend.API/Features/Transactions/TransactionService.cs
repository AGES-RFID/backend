using Backend.Database;
using Backend.Features.Auth;
using Backend.Features.Users;
using Microsoft.EntityFrameworkCore;

namespace Backend.Features.Transactions;

public interface ITransactionService
{
    Task<TransactionDto> CreateTransactionAsync(CreateTransactionDto dto);
    Task<List<TransactionDto>> GetMyTransactionAsync();
}

public class TransactionService(AppDbContext db, IUserService userService, ICurrentUserContext currentUserContext) : ITransactionService
{
    private readonly AppDbContext _db = db;
    private readonly IUserService _userService = userService;
    private readonly ICurrentUserContext _currentUserContext = currentUserContext;

    public async Task<TransactionDto> CreateTransactionAsync(CreateTransactionDto dto)
    {
        var actorUserId = _currentUserContext.GetRequiredUserId();
        var actorRole = _currentUserContext.GetRequiredRole();

        var targetUserId = dto.UserId ?? actorUserId;

        if (actorRole != UserRole.Admin && actorUserId != targetUserId)
            throw new UnauthorizedAccessException("Usuário não autorizado");

        var target = await _userService.GetUserAsync(targetUserId)
            ?? throw new InvalidOperationException("Usuário não encontrado");

        var transaction = await _db.Transactions.AddAsync(new Transaction
        {
            UserId = target.UserId,
            Description = dto.Description,
            Amount = dto.Amount,
            TransactionType = TransactionType.DEPOSIT,
        });

        await _db.SaveChangesAsync();

        return TransactionDto.FromModel(transaction.Entity);
    }

    public async Task<List<TransactionDto>> GetMyTransactionAsync()
    {
        var userId = _currentUserContext.GetRequiredUserId();

        var transactions = await _db.Transactions
            .Where(t => t.UserId == userId)
            .Include(t => t.Access)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        if (transactions == null || transactions.Count == 0)
            return [];

        return [.. transactions.Select(TransactionDto.FromModel)];
    }
}
