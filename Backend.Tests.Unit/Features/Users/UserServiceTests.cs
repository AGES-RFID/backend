using Backend.Database;
using Backend.Features.Users;
using Backend.Features.Vehicles;
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
        var dto = new CreateUserDto { Name = "Alice", Email = "alice@example.com", Password = "password123", Role = UserRole.Admin };

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

        var dto = new CreateUserDto { Name = "Alice2", Email = "alice@example.com", Password = "password123", Role = UserRole.Admin };
        await Assert.ThrowsAsync<EmailAlreadyExistsException>(() => new UserService(db).CreateUserAsync(dto));
    }

    [Fact]
    public async Task UpdateUserAsync_WhenValid_UpdatesAndReturnsUser()
    {
        var db = CreateInMemoryDb();
        var user = CreateUser();
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var dto = new UpdateUserDto { Name = "Alice Updated", Email = "alice2@example.com" };
        var result = await new UserService(db).UpdateUserAsync(user.UserId, dto);

        Assert.Equal("Alice Updated", result.Name);
        Assert.Equal("alice2@example.com", result.Email);
    }

    [Fact]
    public async Task UpdateUserAsync_WhenNotFound_ThrowsKeyNotFoundException()
    {
        var db = CreateInMemoryDb();
        var dto = new UpdateUserDto { Name = "X", Email = "x@example.com" };
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

        var dto = new UpdateUserDto { Name = "Bob", Email = "alice@example.com" };
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

    [Fact]
    public async Task GetUserAsync_WhenUserExists_ReturnsVehicles()
    {
        var db = CreateInMemoryDb();
        var user = CreateUser();
        db.Users.Add(user);
        db.Vehicles.Add(new Vehicle
        {
            UserId = user.UserId,
            Plate = "ABC1234",
            Brand = "toyota",
            Model = "corolla"
        });
        await db.SaveChangesAsync();

        var result = await new UserService(db).GetUserAsync(user.UserId);

        Assert.Single(result.Vehicles);
    }

    [Fact]
    public async Task GetUserAsync_WhenUserHasNoVehicles_ReturnsEmptyVehicles()
    {
        var db = CreateInMemoryDb();
        var user = CreateUser();
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var result = await new UserService(db).GetUserAsync(user.UserId);

        Assert.Empty(result.Vehicles);
    }

    [Fact]
    public async Task UpdateUserAsync_WhenOnlyNameProvided_UpdatesNameOnly()
    {
        var db = CreateInMemoryDb();
        var user = CreateUser();
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var dto = new UpdateUserDto { Name = "Novo Nome" };
        var result = await new UserService(db).UpdateUserAsync(user.UserId, dto);

        Assert.Equal("Novo Nome", result.Name);
        Assert.Equal(user.Email, result.Email);
    }

    [Fact]
    public async Task UpdateUserAsync_WhenOnlyRoleProvided_UpdatesRoleOnly()
    {
        var db = CreateInMemoryDb();
        var user = CreateUser();
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var dto = new UpdateUserDto { Role = UserRole.Customer };
        var result = await new UserService(db).UpdateUserAsync(user.UserId, dto);

        Assert.Equal(UserRole.Customer, result.Role);
        Assert.Equal(user.Name, result.Name);
    }
}