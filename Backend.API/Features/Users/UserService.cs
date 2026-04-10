using Backend.Database;
using Microsoft.EntityFrameworkCore;
namespace Backend.Features.Users;

public interface IUserService
{
    Task<UserDto> GetUserAsync(Guid id);
    Task<IEnumerable<UserDto>> GetAllUsersAsync();
    Task DeleteUserAsync(Guid id);
}

public class UserService(AppDbContext db) : IUserService
{
    private readonly AppDbContext _db = db;

    public async Task<UserDto> GetUserAsync(Guid id)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == id)
            ?? throw new KeyNotFoundException($"User with id {id} not found");

        return UserDto.FromModel(user);
    }

    public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
    {
        var users = await _db.Users.AsNoTracking().Select(u => UserDto.FromModel(u)).ToListAsync();

        return users;
    }

    

   
    public async Task DeleteUserAsync(Guid id)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == id)
            ?? throw new KeyNotFoundException($"User with id {id} not found");

        _db.Users.Remove(user);
        await _db.SaveChangesAsync();
    }
}
