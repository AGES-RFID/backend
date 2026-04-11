using Backend.Database;
using Backend.Features.Users;
using Microsoft.EntityFrameworkCore;

namespace Backend.Tests.Unit.Features.Users;

public class UserServiceTests
{
    private static AppDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static User CreateUser(string name = "Alice", string email = "alice@example.com", UserRole role = UserRole.Admin)
        => new() { Name = name, Email = email, PasswordHash = "hash", Role = role };

    [Fact]
    public async Task GetUserAsync_WhenUserExists_ReturnsUserDto()
    {
        var db = CreateInMemoryDb();
        var user = CreateUser();
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var result = await new UserService(db).GetUserAsync(user.UserId);

        Assert.Equal(user.UserId, result.UserId);
        Assert.Equal(user.Name, result.Name);
    }

    [Fact]
    public async Task GetUserAsync_WhenNotFound_ThrowsKeyNotFoundException()
    {
        var db = CreateInMemoryDb();
        await Assert.ThrowsAsync<KeyNotFoundException>(() => new UserService(db).GetUserAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetUserByNameAsync_WhenUserExists_ReturnsUserDto()
    {
        var db = CreateInMemoryDb();
        var user = CreateUser();
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var result = await new UserService(db).GetUserByNameAsync(user.Name);

        Assert.Equal(user.Name, result.Name);
    }

    [Fact]
    public async Task GetUserByNameAsync_WhenNotFound_ThrowsKeyNotFoundException()
    {
        var db = CreateInMemoryDb();
        await Assert.ThrowsAsync<KeyNotFoundException>(() => new UserService(db).GetUserByNameAsync("inexistente"));
    }

    [Fact]
    public async Task GetAllUsersAsync_WhenEmpty_ReturnsEmptyList()
    {
        var db = CreateInMemoryDb();
        var result = await new UserService(db).GetAllUsersAsync();
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllUsersAsync_WhenUsersExist_ReturnsAll()
    {
        var db = CreateInMemoryDb();
        db.Users.Add(CreateUser("Alice", "alice@example.com"));
        db.Users.Add(CreateUser("Bob", "bob@example.com"));
        await db.SaveChangesAsync();

        var result = await new UserService(db).GetAllUsersAsync();

        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task CreateUserAsync_WhenValid_CreatesAndReturnsUser()
    {
        var db = CreateInMemoryDb();
        var dto = new CreateUserDto { Name = "Alice", Email = "alice@example.com", Password = "password123", Role = "admin" };

        var result = await new UserService(db).CreateUserAsync(dto);

        Assert.Equal(dto.Name, result.Name);
        Assert.Equal(dto.Email, result.Email);
    }

    [Fact]
    public async Task CreateUserAsync_WhenEmailExists_ThrowsEmailAlreadyExistsException()
    {
        var db = CreateInMemoryDb();
        db.Users.Add(CreateUser(email: "alice@example.com"));
        await db.SaveChangesAsync();

        var dto = new CreateUserDto { Name = "Alice2", Email = "alice@example.com", Password = "password123", Role = "admin" };
        await Assert.ThrowsAsync<EmailAlreadyExistsException>(() => new UserService(db).CreateUserAsync(dto));
    }

    [Fact]
    public async Task CreateUserAsync_WhenInvalidRole_ThrowsUserCreationException()
    {
        var db = CreateInMemoryDb();
        var dto = new CreateUserDto { Name = "Alice", Email = "alice@example.com", Password = "password123", Role = "invalido" };
        await Assert.ThrowsAsync<UserCreationException>(() => new UserService(db).CreateUserAsync(dto));
    }

    [Fact]
    public async Task UpdateUserAsync_WhenValid_UpdatesAndReturnsUser()
    {
        var db = CreateInMemoryDb();
        var user = CreateUser();
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var dto = new CreateUserDto { Name = "Alice Updated", Email = "alice2@example.com", Password = "password123", Role = "admin" };
        var result = await new UserService(db).UpdateUserAsync(user.UserId, dto);

        Assert.Equal("Alice Updated", result.Name);
        Assert.Equal("alice2@example.com", result.Email);
    }

    [Fact]
    public async Task UpdateUserAsync_WhenNotFound_ThrowsKeyNotFoundException()
    {
        var db = CreateInMemoryDb();
        var dto = new CreateUserDto { Name = "X", Email = "x@example.com", Password = "password123", Role = "admin" };
        await Assert.ThrowsAsync<KeyNotFoundException>(() => new UserService(db).UpdateUserAsync(Guid.NewGuid(), dto));
    }

    [Fact]
    public async Task UpdateUserAsync_WhenEmailInUse_ThrowsEmailAlreadyExistsException()
    {
        var db = CreateInMemoryDb();
        db.Users.Add(CreateUser("Alice", "alice@example.com"));
        var user2 = CreateUser("Bob", "bob@example.com");
        db.Users.Add(user2);
        await db.SaveChangesAsync();

        var dto = new CreateUserDto { Name = "Bob", Email = "alice@example.com", Password = "password123", Role = "admin" };
        await Assert.ThrowsAsync<EmailAlreadyExistsException>(() => new UserService(db).UpdateUserAsync(user2.UserId, dto));
    }

    [Fact]
    public async Task DeleteUserAsync_WhenUserExists_DeletesUser()
    {
        var db = CreateInMemoryDb();
        var user = CreateUser();
        db.Users.Add(user);
        await db.SaveChangesAsync();

        await new UserService(db).DeleteUserAsync(user.UserId);

        Assert.Null(await db.Users.FindAsync(user.UserId));
    }

    [Fact]
    public async Task DeleteUserAsync_WhenNotFound_ThrowsKeyNotFoundException()
    {
        var db = CreateInMemoryDb();
        await Assert.ThrowsAsync<KeyNotFoundException>(() => new UserService(db).DeleteUserAsync(Guid.NewGuid()));
    }
}