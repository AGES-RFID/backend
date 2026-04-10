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

 
}
