using Backend.Database;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace Backend.Features.Users;

public class UserCreationException : Exception
{
    public UserCreationException(string message) : base(message) { }
}

public class EmailAlreadyExistsException : Exception
{
    public EmailAlreadyExistsException(string email) : base($"Um usuário com o email {email} já existe") { }
}

public interface IUserService
{
    Task<UserDto> GetUserAsync(Guid id);
    Task<IEnumerable<UserDto>> GetAllUsersAsync();
    Task<UserDto> CreateUserAsync(CreateUserDto dto);
    Task<UserDto> UpdateUserAsync(Guid id, CreateUserDto dto);
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

    public async Task<UserDto> CreateUserAsync(CreateUserDto dto)
    {
        // Validate role
        if (!Enum.TryParse<UserRole>(dto.Role, ignoreCase: true, out var role))
        {
            throw new UserCreationException($"Invalid role '{dto.Role}'. Valid roles are: {string.Join(", ", Enum.GetNames(typeof(UserRole)))}");
        }

        // Check if email already exists
        var existingUser = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (existingUser != null)
        {
            throw new EmailAlreadyExistsException(dto.Email);
        }

        // Hash the password
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

        var user = await _db.Users.AddAsync(new User
        {
            Name = dto.Name,
            Email = dto.Email,
            PasswordHash = passwordHash,
            Role = role
        });

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

    public async Task DeleteUserAsync(Guid id)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == id)
            ?? throw new KeyNotFoundException($"User with id {id} not found");

        _db.Users.Remove(user);
        await _db.SaveChangesAsync();
    }
}
