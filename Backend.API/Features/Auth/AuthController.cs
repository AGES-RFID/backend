using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Backend.Features.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Features.Auth;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService, IUserService userService) : ControllerBase
{
    private readonly IAuthService _authService = authService;
    private readonly IUserService _userService = userService;

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login(LoginDto dto)
    {
        try
        {
            var response = await _authService.LoginAsync(dto);
            return Ok(response);
        }
        catch (InvalidCredentialsException)
        {
            return Unauthorized(new { error = "Email ou senha inválidos" });
        }
    }


    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserWithVehiclesDto>> GetCurrentUser()
    {
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (!Guid.TryParse(sub, out var userId))
            return Unauthorized(new { error = "Token inválido" });

        var user = await _userService.GetUserAsync(userId);
        if (user == null)
            return NotFound(new { error = "Usuário não encontrado" });

        return Ok(user);
    }

}
