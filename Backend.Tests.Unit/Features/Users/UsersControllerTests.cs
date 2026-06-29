using Backend.Features.Auth;
using Backend.Features.Users;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Backend.Tests.Unit.Features.Users;

public class UsersControllerTests
{
    private static UsersController CreateController(IUserService userService)
        => CreateController(userService, CreateCurrentUserContext(true, true, Guid.NewGuid()));

    private static UsersController CreateController(IUserService userService, ICurrentUserContext currentUserContext)
        => new(userService, currentUserContext);

    private static ICurrentUserContext CreateCurrentUserContext(bool isAuthenticated, bool isAdmin, Guid? userId = null)
    {
        var currentUserContext = Substitute.For<ICurrentUserContext>();
        currentUserContext.IsAuthenticated.Returns(isAuthenticated);
        currentUserContext.IsAdmin.Returns(isAdmin);
        currentUserContext.Role.Returns(isAdmin ? UserRole.Admin : UserRole.Customer);
        currentUserContext.UserId.Returns(userId);
        if (userId.HasValue)
        {
            currentUserContext.GetRequiredUserId().Returns(userId.Value);
        }

        return currentUserContext;
    }

    [Fact]
    public async Task GetUser_WhenServiceThrowsKeyNotFoundException_ReturnsNotFound()
    {
        var userId = Guid.NewGuid();
        var userService = Substitute.For<IUserService>();
        userService.GetUserAsync(userId)
            .Returns(Task.FromException<UserWithVehiclesDto>(new KeyNotFoundException("not found")));

        var controller = CreateController(userService);
        var result = await controller.GetUser(userId);

        Assert.IsType<NotFoundResult>(result.Result);
        await userService.Received(1).GetUserAsync(userId);
    }

    [Fact]
    public async Task GetUser_WhenServiceReturnsUser_ReturnsOkWithUser()
    {
        var userId = Guid.NewGuid();
        var expected = new UserWithVehiclesDto { UserId = userId, Name = "Alice", Email = "alice@example.com", Role = UserRole.Admin };

        var userService = Substitute.For<IUserService>();
        userService.GetUserAsync(userId).Returns(expected);

        var controller = CreateController(userService);
        var result = await controller.GetUser(userId);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<UserWithVehiclesDto>(ok.Value);
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
        var dto = new UpdateUserDto { Name = "Bob", Email = "bob@example.com" };

        var userService = Substitute.For<IUserService>();
        userService.UpdateUserAsync(userId, Arg.Any<UpdateUserDto>())
            .Returns(Task.FromException<UserDto>(new KeyNotFoundException("not found")));

        var controller = CreateController(userService);
        var result = await controller.UpdateUser(userId, dto);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task UpdateUser_WhenServiceSucceeds_ReturnsOk()
    {
        var userId = Guid.NewGuid();
        var dto = new UpdateUserDto { Name = "Carol", Email = "carol@example.com" };

        var userService = Substitute.For<IUserService>();
        userService.UpdateUserAsync(userId, Arg.Any<UpdateUserDto>())
            .Returns(Task.FromResult(new UserDto { UserId = userId, Name = dto.Name ?? "existing", Email = dto.Email ?? "existing@email.com", Role = UserRole.Admin }));

        var controller = CreateController(userService);
        var result = await controller.UpdateUser(userId, dto);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task UpdateUser_WhenCustomerUpdatesAnotherUser_ReturnsForbid()
    {
        var currentUserContext = CreateCurrentUserContext(true, false, Guid.NewGuid());
        var userService = Substitute.For<IUserService>();

        var controller = CreateController(userService, currentUserContext);
        var result = await controller.UpdateUser(Guid.NewGuid(), new UpdateUserDto { Name = "Blocked" });

        Assert.IsType<ForbidResult>(result);
        await userService.DidNotReceiveWithAnyArgs().UpdateUserAsync(default, default!);
    }

    [Fact]
    public async Task UpdateUser_WhenCustomerUpdatesSelf_RemovesRoleEscalation()
    {
        var userId = Guid.NewGuid();
        var dto = new UpdateUserDto { Name = "Self", Role = UserRole.Admin };
        UpdateUserDto? capturedDto = null;

        var currentUserContext = CreateCurrentUserContext(true, false, userId);
        var userService = Substitute.For<IUserService>();
        userService.UpdateUserAsync(userId, Arg.Do<UpdateUserDto>(x => capturedDto = x))
            .Returns(new UserDto { UserId = userId, Name = dto.Name, Email = "self@example.com", Role = UserRole.Customer });

        var controller = CreateController(userService, currentUserContext);
        var result = await controller.UpdateUser(userId, dto);

        Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(capturedDto);
        Assert.Null(capturedDto.Role);
    }

    [Fact]
    public async Task GetAllUsers_WhenServiceReturnsUsers_ReturnsOkWithList()
    {
        var userService = Substitute.For<IUserService>();
        userService.GetAllUsersAsync().Returns(
        [
            new() { UserId = Guid.NewGuid(), Name = "Alice", Email = "alice@example.com", Role = UserRole.Admin },
             new() { UserId = Guid.NewGuid(), Name = "Bob", Email = "bob@example.com", Role = UserRole.Admin }
        ]);

        var controller = CreateController(userService);
        var result = await controller.GetAllUsers();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsType<IEnumerable<UserDto>>(ok.Value, exactMatch: false);
        Assert.Equal(2, list.Count());
    }

    [Fact]
    public async Task CreateUser_WhenServiceSucceeds_ReturnsCreated()
    {
        var dto = new CreateUserDto { Name = "Alice", Email = "alice@example.com", Password = "password123", Role = UserRole.Admin };
        var created = new UserDto { UserId = Guid.NewGuid(), Name = dto.Name, Email = dto.Email, Role = dto.Role };

        var userService = Substitute.For<IUserService>();
        userService.CreateUserAsync(Arg.Any<CreateUserDto>()).Returns(created);

        var controller = CreateController(userService);
        var result = await controller.CreateUser(dto);

        Assert.IsType<CreatedAtActionResult>(result.Result);
    }

    [Fact]
    public async Task CreateUser_WhenAnonymous_ForcesCustomerRole()
    {
        var dto = new CreateUserDto { Name = "Alice", Email = "alice@example.com", Password = "password123", Role = UserRole.Admin };
        CreateUserDto? capturedDto = null;

        var currentUserContext = CreateCurrentUserContext(false, false);
        var userService = Substitute.For<IUserService>();
        userService.CreateUserAsync(Arg.Do<CreateUserDto>(x => capturedDto = x))
            .Returns(call =>
            {
                var createDto = call.Arg<CreateUserDto>();
                return new UserDto { UserId = Guid.NewGuid(), Name = createDto.Name, Email = createDto.Email, Role = createDto.Role };
            });

        var controller = CreateController(userService, currentUserContext);
        var result = await controller.CreateUser(dto);

        Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.NotNull(capturedDto);
        Assert.Equal(UserRole.Customer, capturedDto.Role);
    }

    [Fact]
    public async Task CreateUser_WhenCustomerAuthenticated_ReturnsForbid()
    {
        var currentUserContext = CreateCurrentUserContext(true, false, Guid.NewGuid());
        var userService = Substitute.For<IUserService>();

        var controller = CreateController(userService, currentUserContext);
        var result = await controller.CreateUser(new CreateUserDto { Name = "Blocked", Email = "blocked@example.com", Password = "password123", Role = UserRole.Customer });

        Assert.IsType<ForbidResult>(result.Result);
        await userService.DidNotReceiveWithAnyArgs().CreateUserAsync(default!);
    }

    [Fact]
    public async Task CreateUser_WhenEmailAlreadyExists_ReturnsConflict()
    {
        var dto = new CreateUserDto { Name = "Alice", Email = "alice@example.com", Password = "password123", Role = UserRole.Admin };

        var userService = Substitute.For<IUserService>();
        userService.CreateUserAsync(Arg.Any<CreateUserDto>())
            .Returns(Task.FromException<UserDto>(new EmailAlreadyExistsException(dto.Email)));

        var controller = CreateController(userService);
        var result = await controller.CreateUser(dto);

        Assert.IsType<ConflictObjectResult>(result.Result);
    }

    [Fact]
    public async Task DeleteUser_WhenUserExists_ReturnsNoContent()
    {
        var userId = Guid.NewGuid();
        var userService = Substitute.For<IUserService>();
        userService.DeleteUserAsync(userId).Returns(Task.CompletedTask);

        var controller = CreateController(userService);
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

        var controller = CreateController(userService);
        var result = await controller.DeleteUser(userId);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetUserByName_WhenUserExists_ReturnsOk()
    {
        var userService = Substitute.For<IUserService>();
        userService.GetUserByNameAsync("Alice")
            .Returns(new UserWithVehiclesDto { UserId = Guid.NewGuid(), Name = "Alice", Email = "alice@example.com", Role = UserRole.Admin });

        var controller = CreateController(userService);
        var result = await controller.GetUserByName("Alice");

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetUserByName_WhenUserNotFound_ReturnsNotFound()
    {
        var userService = Substitute.For<IUserService>();
        userService.GetUserByNameAsync("Inexistente")
            .Returns(Task.FromException<UserWithVehiclesDto>(new KeyNotFoundException()));

        var controller = CreateController(userService);
        var result = await controller.GetUserByName("Inexistente");

        Assert.IsType<NotFoundResult>(result.Result);
    }
}
