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
    {
        return new User
        {
            Name = name,
            Email = email,
            PasswordHash = "hash",
            Role = role
        };
    }

    // GetUserAsync
    [Fact]
    public async Task GetUserAsync_WhenUserExists_ReturnsUserDto()
    {
        var db = CreateInMemoryDb();
        var user = CreateUser();
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var service = new UserService(db);
        var result = await service.GetUserAsync(user.UserId);

        Assert.Equal(user.UserId, result.UserId);
        Assert.Equal(user.Name, result.Name);
        Assert.Equal(user.Email, result.Email);
    }

    [Fact]
    public async Task GetUserAsync_WhenUserNotFound_ThrowsKeyNotFoundException()
    {
        var db = CreateInMemoryDb();
        var service = new UserService(db);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetUserAsync(Guid.NewGuid()));
    }

    // GetUserByNameAsync
    [Fact]
    public async Task GetUserByNameAsync_WhenUserExists_ReturnsUserDto()
    {
        var db = CreateInMemoryDb();
        var user = CreateUser();
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var service = new UserService(db);
        var result = await service.GetUserByNameAsync(user.Name);

        Assert.Equal(user.Name, result.Name);
    }

    [Fact]
    public async Task GetUserByNameAsync_WhenUserNotFound_ThrowsKeyNotFoundException()
    {
        var db = CreateInMemoryDb();
        var service = new UserService(db);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetUserByNameAsync("inexistente"));
    }

    // GetAllUsersAsync
    [Fact]
    public async Task GetAllUsersAsync_WhenNoUsers_ReturnsEmptyList()
    {
        var db = CreateInMemoryDb();
        var service = new UserService(db);

        var result = await service.GetAllUsersAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllUsersAsync_WhenUsersExist_ReturnsAllUsers()
    {
        var db = CreateInMemoryDb();
        db.Users.Add(CreateUser("Alice", "alice@example.com"));
        db.Users.Add(CreateUser("Bob", "bob@example.com"));
        await db.SaveChangesAsync();

        var service = new UserService(db);
        var result = await service.GetAllUsersAsync();

        Assert.Equal(2, result.Count());
    }

     // CreateUserAsync
     [Fact]
     public async Task CreateUserAsync_WhenValidData_CreatesUser()
     {
         var db = CreateInMemoryDb();
         var service = new UserService(db);

         var dto = new CreateUserDto { Name = "Alice", Email = "alice@example.com", Password = "password123", Role = UserRole.Admin };
         var result = await service.CreateUserAsync(dto);

         Assert.Equal(dto.Name, result.Name);
         Assert.Equal(dto.Email, result.Email);
         Assert.Equal(UserRole.Admin, result.Role);
     }

     [Fact]
     public async Task CreateUserAsync_WhenEmailAlreadyExists_ThrowsEmailAlreadyExistsException()
     {
         var db = CreateInMemoryDb();
         db.Users.Add(CreateUser(email: "alice@example.com"));
         await db.SaveChangesAsync();

         var service = new UserService(db);
         var dto = new CreateUserDto { Name = "Alice2", Email = "alice@example.com", Password = "password123", Role = UserRole.Admin };

         await Assert.ThrowsAsync<EmailAlreadyExistsException>(() => service.CreateUserAsync(dto));
     }

     // UpdateUserAsync
     [Fact]
     public async Task UpdateUserAsync_WhenUserExists_UpdatesAndReturnsUser()
     {
         var db = CreateInMemoryDb();
         var user = CreateUser();
         db.Users.Add(user);
         await db.SaveChangesAsync();

          var service = new UserService(db);
          var dto = new UpdateUserDto { Name = "Alice Updated", Email = "alice2@example.com" };
          var result = await service.UpdateUserAsync(user.UserId, dto);

          Assert.Equal("Alice Updated", result.Name);
          Assert.Equal("alice2@example.com", result.Email);
      }

      [Fact]
      public async Task UpdateUserAsync_WhenUserNotFound_ThrowsKeyNotFoundException()
      {
          var db = CreateInMemoryDb();
          var service = new UserService(db);

          var dto = new UpdateUserDto { Name = "X", Email = "x@example.com" };

          await Assert.ThrowsAsync<KeyNotFoundException>(() => service.UpdateUserAsync(Guid.NewGuid(), dto));
      }

      [Fact]
      public async Task UpdateUserAsync_WhenEmailInUseByAnother_ThrowsEmailAlreadyExistsException()
      {
          var db = CreateInMemoryDb();
          db.Users.Add(CreateUser("Alice", "alice@example.com"));
          var user2 = CreateUser("Bob", "bob@example.com");
          db.Users.Add(user2);
          await db.SaveChangesAsync();

          var service = new UserService(db);
          var dto = new UpdateUserDto { Name = "Bob", Email = "alice@example.com" };

          await Assert.ThrowsAsync<EmailAlreadyExistsException>(() => service.UpdateUserAsync(user2.UserId, dto));
      }

    // DeleteUserAsync
    [Fact]
    public async Task DeleteUserAsync_WhenUserExists_DeletesUser()
    {
        var db = CreateInMemoryDb();
        var user = CreateUser();
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var service = new UserService(db);
        await service.DeleteUserAsync(user.UserId);

        Assert.Null(await db.Users.FindAsync(user.UserId));
    }

    [Fact]
    public async Task DeleteUserAsync_WhenUserNotFound_ThrowsKeyNotFoundException()
    {
        var db = CreateInMemoryDb();
        var service = new UserService(db);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.DeleteUserAsync(Guid.NewGuid()));
    }
}