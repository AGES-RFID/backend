using Backend.Features.Users;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Backend.Tests.Unit.Features.Users;

public class UsersControllerTests
{
    [Fact]
    public async Task GetUser_WhenServiceThrowsKeyNotFoundException_ReturnsNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();

        var userService = Substitute.For<IUserService>();
        userService.GetUserAsync(userId)
            .Returns(Task.FromException<UserDto>(new KeyNotFoundException("not found")));

        var controller = new UsersController(userService);

        // Act
        var result = await controller.GetUser(userId);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
        await userService.Received(1).GetUserAsync(userId);
    }

    [Fact]
    public async Task GetUser_WhenServiceReturnsUser_ReturnsOkWithUser()
    {
        // Arrange
        var userId = Guid.NewGuid();

        var expected = new UserDto
        {
            UserId = userId,
            Name = "Alice",
            Email = "alice@example.com"
        };

        var userService = Substitute.For<IUserService>();
        userService.GetUserAsync(userId).Returns(expected);

        var controller = new UsersController(userService);

        // Act
        var result = await controller.GetUser(userId);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<UserDto>(ok.Value);

        Assert.Equal(expected.UserId, dto.UserId);
        Assert.Equal(expected.Name, dto.Name);
        Assert.Equal(expected.Email, dto.Email);

        await userService.Received(1).GetUserAsync(userId);
    }

    [Fact]
    public async Task UpdateUser_WhenServiceThrowsKeyNotFoundException_ReturnsNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var dto = new CreateUserDto { Name = "Bob", Email = "bob@example.com", PasswordHash = "hash", Cpf = "12345678901", PhoneNumber = "5551999990000" };

        var userService = Substitute.For<IUserService>();
        userService
            .UpdateUserAsync(userId, Arg.Any<CreateUserDto>())
            .Returns(Task.FromException<UserDto>(new KeyNotFoundException("not found")));

        var controller = new UsersController(userService);

        // Act
        var result = await controller.UpdateUser(userId, dto);

        // Assert
        Assert.IsType<NotFoundResult>(result);
        await userService.Received(1).UpdateUserAsync(
            userId,
            Arg.Is<CreateUserDto>(d => d.Name == dto.Name && d.Email == dto.Email)
        );
    }

    [Fact]
    public async Task UpdateUser_WhenServiceSucceeds_ReturnsNoContent()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var dto = new CreateUserDto { Name = "Carol", Email = "carol@example.com", PasswordHash = "hash", Cpf = "12345678901", PhoneNumber = "5551999990000" };

        var userService = Substitute.For<IUserService>();
        userService.UpdateUserAsync(userId, Arg.Any<CreateUserDto>())
            .Returns(Task.FromResult(new UserDto { UserId = userId, Name = dto.Name, Email = dto.Email }));

        var controller = new UsersController(userService);

        // Act
        var result = await controller.UpdateUser(userId, dto);

        // Assert
        Assert.IsType<NoContentResult>(result);
        await userService.Received(1).UpdateUserAsync(
            userId,
            Arg.Is<CreateUserDto>(d => d.Name == dto.Name && d.Email == dto.Email)
        );
    }
}
