using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Backend.Database;
using Backend.Features.Users;
using Backend.Features.Transactions;
using Microsoft.EntityFrameworkCore;

namespace Backend.Features.Transactions;

public interface ITransactionService
{
    Task<TransactionResponseDto> CreateTransactionAsync(CreateTransactionDto dto);
}

public class TransactionService(AppDbContext db, IHttpContextAccessor httpContextAccessor, IUserService userService) : ITransactionService
{
    private readonly AppDbContext _db = db;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly IUserService _userService = userService;

    public async Task<TransactionResponseDto> CreateTransactionAsync(CreateTransactionDto dto)
    {
        var sub = _httpContextAccessor.HttpContext?.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (!Guid.TryParse(sub, out var userId))
            throw new InvalidOperationException("Token inválido");

        var user = await _userService.GetUserAsync(userId);
        if (user == null)
            throw new InvalidOperationException("Usuário não encontrado");

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
