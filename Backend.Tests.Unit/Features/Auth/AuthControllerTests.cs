using Backend.Features.Auth;
using Backend.Features.Users;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Backend.Tests.Unit.Features.Auth;

public class AuthControllerTests
{
    [Fact]
    public async Task Login_WhenCredentialsValid_ReturnsOkWithAuthResponse()
    {
        var dto = new LoginDto { Email = "user@example.com", Password = "password123" };
        var expectedUser = new UserDto { UserId = Guid.NewGuid(), Name = "Test User", Email = dto.Email, Role = UserRole.Admin };
        var expectedResponse = new AuthResponse { Token = "valid.jwt.token", User = expectedUser };

        var authService = Substitute.For<IAuthService>();
        authService.LoginAsync(dto).Returns(expectedResponse);

        var userService = Substitute.For<IUserService>();

        var controller = new AuthController(authService, userService);
        var result = await controller.Login(dto);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<AuthResponse>(okResult.Value);
        Assert.Equal(expectedResponse.Token, response.Token);
        Assert.Equal(expectedResponse.User.Email, response.User.Email);
        await authService.Received(1).LoginAsync(dto);
    }

    [Fact]
    public async Task Login_WhenEmailNotFound_ReturnsUnauthorized()
    {
        var dto = new LoginDto { Email = "notfound@example.com", Password = "password123" };

        var authService = Substitute.For<IAuthService>();
        authService.LoginAsync(dto).Returns(Task.FromException<AuthResponse>(
            new InvalidCredentialsException()));

        var userService = Substitute.For<IUserService>();

        var controller = new AuthController(authService, userService);
        var result = await controller.Login(dto);

        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        var error = unauthorizedResult.Value;
        Assert.NotNull(error);
        await authService.Received(1).LoginAsync(dto);
    }

    [Fact]
    public async Task Login_WhenPasswordInvalid_ReturnsUnauthorized()
    {
        var dto = new LoginDto { Email = "user@example.com", Password = "wrongpassword" };

        var authService = Substitute.For<IAuthService>();
        authService.LoginAsync(dto).Returns(Task.FromException<AuthResponse>(
            new InvalidCredentialsException()));

        var userService = Substitute.For<IUserService>();

        var controller = new AuthController(authService, userService);
        var result = await controller.Login(dto);

        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        Assert.NotNull(unauthorizedResult.Value);
        await authService.Received(1).LoginAsync(dto);
    }

    [Fact]
    public async Task Login_ReturnsValidJwtToken()
    {
        var dto = new LoginDto { Email = "user@example.com", Password = "password123" };
        var expectedUser = new UserDto { UserId = Guid.NewGuid(), Name = "Test User", Email = dto.Email, Role = UserRole.User };
        var expectedResponse = new AuthResponse { Token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.abc.def", User = expectedUser };

        var authService = Substitute.For<IAuthService>();
        authService.LoginAsync(dto).Returns(expectedResponse);

        var userService = Substitute.For<IUserService>();

        var controller = new AuthController(authService, userService);
        var result = await controller.Login(dto);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<AuthResponse>(okResult.Value);
        Assert.NotEmpty(response.Token);
        Assert.StartsWith("eyJ", response.Token); // JWT token signature
    }

    [Fact]
    public async Task GetCurrentUser_WhenAuthorized_ReturnsUserWithVehicles()
    {
        var userId = Guid.NewGuid();
        var expected = new UserWithVehiclesDto
        {
            UserId = userId,
            Name = "Test User",
            Email = "user@example.com",
            Role = UserRole.Admin,
            Vehicles = new List<VehicleDto>()
        };

        var authService = Substitute.For<IAuthService>();
        var userService = Substitute.For<IUserService>();
        userService.GetUserAsync(userId).Returns(expected);

        var controller = new AuthController(authService, userService);
        controller.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        controller.ControllerContext.HttpContext.User = new System.Security.Claims.ClaimsPrincipal(
            new System.Security.Claims.ClaimsIdentity(
                new[] { new System.Security.Claims.Claim(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub, userId.ToString()) }
            )
        );

        var result = await controller.GetCurrentUser();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var user = Assert.IsType<UserWithVehiclesDto>(okResult.Value);
        Assert.Equal(expected.UserId, user.UserId);
        Assert.Equal(expected.Email, user.Email);
        await userService.Received(1).GetUserAsync(userId);
    }

    [Fact]
    public async Task GetCurrentUser_WhenTokenInvalid_ReturnsUnauthorized()
    {
        var authService = Substitute.For<IAuthService>();
        var userService = Substitute.For<IUserService>();

        var controller = new AuthController(authService, userService);
        controller.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        controller.ControllerContext.HttpContext.User = new System.Security.Claims.ClaimsPrincipal(
            new System.Security.Claims.ClaimsIdentity(
                new[] { new System.Security.Claims.Claim(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub, "invalid-guid") }
            )
        );

        var result = await controller.GetCurrentUser();

        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        Assert.NotNull(unauthorizedResult.Value);
    }

    [Fact]
    public async Task GetCurrentUser_WhenUserNotFound_ReturnsNotFound()
    {
        var userId = Guid.NewGuid();

        var authService = Substitute.For<IAuthService>();
        var userService = Substitute.For<IUserService>();
        userService.GetUserAsync(userId).Returns(Task.FromResult<UserWithVehiclesDto>(null!));

        var controller = new AuthController(authService, userService);
        controller.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        controller.ControllerContext.HttpContext.User = new System.Security.Claims.ClaimsPrincipal(
            new System.Security.Claims.ClaimsIdentity(
                new[] { new System.Security.Claims.Claim(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub, userId.ToString()) }
            )
        );

        var result = await controller.GetCurrentUser();

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.NotNull(notFoundResult.Value);
    }
}
