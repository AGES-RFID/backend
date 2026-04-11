using Backend.Features.Users;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Backend.Tests.Unit.Features.Users;

public class UsersControllerTests
{
    [Fact]
    public async Task GetUser_WhenServiceThrowsKeyNotFoundException_ReturnsNotFound()
    {
        var userId = Guid.NewGuid();
        var userService = Substitute.For<IUserService>();
        userService.GetUserAsync(userId)
            .Returns(Task.FromException<UserDto>(new KeyNotFoundException("not found")));

        var controller = new UsersController(userService);
        var result = await controller.GetUser(userId);

        Assert.IsType<NotFoundResult>(result.Result);
        await userService.Received(1).GetUserAsync(userId);
    }

    [Fact]
    public async Task GetUser_WhenServiceReturnsUser_ReturnsOkWithUser()
    {
        var userId = Guid.NewGuid();
        var expected = new UserDto { UserId = userId, Name = "Alice", Email = "alice@example.com", Role = "admin" };

        var userService = Substitute.For<IUserService>();
        userService.GetUserAsync(userId).Returns(expected);

        var controller = new UsersController(userService);
        var result = await controller.GetUser(userId);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<UserDto>(ok.Value);
        Assert.Equal(expected.UserId, dto.UserId);
        Assert.Equal(expected.Name, dto.Name);
        Assert.Equal(expected.Email, dto.Email);
        Assert.Equal(expected.Role, dto.Role);
        await userService.Received(1).GetUserAsync(userId);
    }

    [Fact]
    public async Task UpdateUser_WhenServiceThrowsKeyNotFoundException_ReturnsNotFound()
    {
        var userId = Guid.NewGuid();
        var dto = new CreateUserDto { Name = "Bob", Email = "bob@example.com", Password = "password123", Role = "admin" };

        var userService = Substitute.For<IUserService>();
        userService.UpdateUserAsync(userId, Arg.Any<CreateUserDto>())
            .Returns(Task.FromException<UserDto>(new KeyNotFoundException("not found")));

        var controller = new UsersController(userService);
        var result = await controller.UpdateUser(userId, dto);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task UpdateUser_WhenServiceSucceeds_ReturnsOk()
    {
        var userId = Guid.NewGuid();
        var dto = new CreateUserDto { Name = "Carol", Email = "carol@example.com", Password = "password123", Role = "admin" };

        var userService = Substitute.For<IUserService>();
        userService.UpdateUserAsync(userId, Arg.Any<CreateUserDto>())
            .Returns(Task.FromResult(new UserDto { UserId = userId, Name = dto.Name, Email = dto.Email, Role = dto.Role }));

        var controller = new UsersController(userService);
        var result = await controller.UpdateUser(userId, dto);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetAllUsers_WhenServiceReturnsUsers_ReturnsOkWithList()
    {
        var userService = Substitute.For<IUserService>();
        userService.GetAllUsersAsync().Returns(new List<UserDto>
        {
            new() { UserId = Guid.NewGuid(), Name = "Alice", Email = "alice@example.com", Role = "admin" },
            new() { UserId = Guid.NewGuid(), Name = "Bob", Email = "bob@example.com", Role = "admin" }
        });

        var controller = new UsersController(userService);
        var result = await controller.GetAllUsers();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsAssignableFrom<IEnumerable<UserDto>>(ok.Value);
        Assert.Equal(2, list.Count());
    }

    [Fact]
    public async Task CreateUser_WhenServiceSucceeds_ReturnsCreated()
    {
        var dto = new CreateUserDto { Name = "Alice", Email = "alice@example.com", Password = "password123", Role = "admin" };
        var created = new UserDto { UserId = Guid.NewGuid(), Name = dto.Name, Email = dto.Email, Role = dto.Role };

        var userService = Substitute.For<IUserService>();
        userService.CreateUserAsync(Arg.Any<CreateUserDto>()).Returns(created);

        var controller = new UsersController(userService);
        var result = await controller.CreateUser(dto);

        Assert.IsType<CreatedAtActionResult>(result.Result);
    }

    [Fact]
    public async Task CreateUser_WhenEmailAlreadyExists_ReturnsConflict()
    {
        var dto = new CreateUserDto { Name = "Alice", Email = "alice@example.com", Password = "password123", Role = "admin" };

        var userService = Substitute.For<IUserService>();
        userService.CreateUserAsync(Arg.Any<CreateUserDto>())
            .Returns(Task.FromException<UserDto>(new EmailAlreadyExistsException(dto.Email)));

        var controller = new UsersController(userService);
        var result = await controller.CreateUser(dto);

        Assert.IsType<ConflictObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateUser_WhenInvalidRole_ReturnsBadRequest()
    {
        var dto = new CreateUserDto { Name = "Alice", Email = "alice@example.com", Password = "password123", Role = "invalido" };

        var userService = Substitute.For<IUserService>();
        userService.CreateUserAsync(Arg.Any<CreateUserDto>())
            .Returns(Task.FromException<UserDto>(new UserCreationException("Invalid role")));

        var controller = new UsersController(userService);
        var result = await controller.CreateUser(dto);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task DeleteUser_WhenUserExists_ReturnsNoContent()
    {
        var userId = Guid.NewGuid();
        var userService = Substitute.For<IUserService>();
        userService.DeleteUserAsync(userId).Returns(Task.CompletedTask);

        var controller = new UsersController(userService);
        var result = await controller.DeleteUser(userId);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task DeleteUser_WhenUserNotFound_ReturnsNotFound()
    {
        var userId = Guid.NewGuid();
        var userService = Substitute.For<IUserService>();
        userService.DeleteUserAsync(userId)
            .Returns(Task.FromException(new KeyNotFoundException()));

        var controller = new UsersController(userService);
        var result = await controller.DeleteUser(userId);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetUserByName_WhenUserExists_ReturnsOk()
    {
        var userService = Substitute.For<IUserService>();
        userService.GetUserByNameAsync("Alice")
            .Returns(new UserDto { UserId = Guid.NewGuid(), Name = "Alice", Email = "alice@example.com", Role = "admin" });

        var controller = new UsersController(userService);
        var result = await controller.GetUserByName("Alice");

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetUserByName_WhenUserNotFound_ReturnsNotFound()
    {
        var userService = Substitute.For<IUserService>();
        userService.GetUserByNameAsync("Inexistente")
            .Returns(Task.FromException<UserDto>(new KeyNotFoundException()));

        var controller = new UsersController(userService);
        var result = await controller.GetUserByName("Inexistente");

        Assert.IsType<NotFoundResult>(result.Result);
    }
}