using Backend.Database;
using Microsoft.EntityFrameworkCore;
namespace Backend.Features.Users;

public interface IUserService
{
    Task<UserDto> GetUserAsync(Guid id);
    Task<IEnumerable<UserDto>> GetAllUsersAsync();
    Task<UserDto> CreateUserAsync(CreateUserDto dto);
    Task<UserDto> UpdateUserAsync(Guid id, CreateUserDto dto);
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

    public async Task<UserDto> CreateUserAsync(CreateUserDto dto)
    {
        var user = await _db.Users.AddAsync(new User { Name = dto.Name, Email = dto.Email });

        await _db.SaveChangesAsync();

        return UserDto.FromModel(user.Entity);
    }

    public async Task<UserDto> UpdateUserAsync(Guid id, CreateUserDto dto)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == id)
            ?? throw new KeyNotFoundException($"User with id {id} not found");

        user.Name = dto.Name;
        user.Email = dto.Email;
        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return UserDto.FromModel(user);
    }

}
