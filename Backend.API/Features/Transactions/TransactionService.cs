using Backend.Database;
using Backend.Features.Users;
using Backend.Features.Transactions;
using Microsoft.EntityFrameworkCore;

namespace Backend.Features.Transactions;

public interface ITransactionService
{
    Task<TransactionResponseDto> CreateTransactionAsync(CreateTransactionDto dto);
}

public class TransactionService(AppDbContext db) : ITransactionService
{
    private readonly AppDbContext _db = db;

    public async Task<TransactionResponseDto> CreateTransactionAsync(CreateTransactionDto dto)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == dto.UserId)
            ?? throw new KeyNotFoundException($"Usuário com o id {dto.UserId} não foi encontrado");

        var transaction = await _db.Transactions.AddAsync(new Transaction
        {
            TransactionId = Guid.NewGuid(),
            UserId = user.UserId,
            Description = dto.Description,
            Amount = dto.Amount,
            TransactionType = TransactionType.DEPOSIT,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();

        return new TransactionResponseDto
        {
            TransactionId = transaction.Entity.TransactionId,
            UserId = transaction.Entity.UserId,
            Amount = transaction.Entity.Amount,
            Description = transaction.Entity.Description,
            TransactionType = transaction.Entity.TransactionType,
            CreatedAt = transaction.Entity.CreatedAt
        };
    }
}
