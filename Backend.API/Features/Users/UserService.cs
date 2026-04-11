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
    Task<UserDto> GetUserByNameAsync(string name);
    Task<UserDto> GetUserAsync(Guid id);
    Task<IEnumerable<UserDto>> GetAllUsersAsync();
    Task<UserDto> CreateUserAsync(CreateUserDto dto);
    Task<UserDto> UpdateUserAsync(Guid id, UpdateUserDto dto);
    Task DeleteUserAsync(Guid id);
}

public class UserService(AppDbContext db) : IUserService
{
    private readonly AppDbContext _db = db;

    public async Task<UserDto> GetUserAsync(Guid id)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == id)
            ?? throw new KeyNotFoundException($"Usuário com o id {id} não foi encontrado");

        return UserDto.FromModel(user);
    }

    public async Task<UserDto> GetUserByNameAsync(string name)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Name == name)
            ?? throw new KeyNotFoundException($"Usuário com o nome {name} não foi encontrado");

        return UserDto.FromModel(user);
    }

    public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
    {
        var users = await _db.Users.AsNoTracking().Select(u => UserDto.FromModel(u)).ToListAsync();

        return users;
    }

    public async Task<UserDto> CreateUserAsync(CreateUserDto dto)
    {
        var existingUser = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (existingUser != null)
        {
            throw new EmailAlreadyExistsException(dto.Email);
        }

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

        var user = await _db.Users.AddAsync(new User
        {
            Name = dto.Name,
            Email = dto.Email,
            PasswordHash = passwordHash,
            Role = dto.Role
        });

        await _db.SaveChangesAsync();

        return UserDto.FromModel(user.Entity);
    }

    public async Task<UserDto> UpdateUserAsync(Guid id, UpdateUserDto dto)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == id)
            ?? throw new KeyNotFoundException($"User with id {id} not found");

        if (!string.IsNullOrEmpty(dto.Email))
        {
            var emailInUse = await _db.Users.AnyAsync(u => u.Email == dto.Email && u.UserId != id);
            if (emailInUse) throw new EmailAlreadyExistsException(dto.Email);
        }

        user.Name = dto.Name ?? user.Name;
        user.Email = dto.Email ?? user.Email;
        user.Role = dto.Role ?? user.Role;
        if (!string.IsNullOrEmpty(dto.Password))
        {
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
        }

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
