using Backend.Features.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Features.Users;

[ApiController]
[Route("api/users")]
public class UsersController(IUserService userService, ICurrentUserContext currentUserContext) : ControllerBase
{
    private readonly IUserService _userService = userService;
    private readonly ICurrentUserContext _currentUserContext = currentUserContext;

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserWithVehiclesDto>>> GetAllUsers()
    {
        var users = await _userService.GetAllUsersAsync();
        return Ok(users);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("by-name/{name}")]
    public async Task<ActionResult<UserWithVehiclesDto>> GetUserByName(string name)
    {
        try
        {
            var user = await _userService.GetUserByNameAsync(name);
            return Ok(user);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("{userId}")]
    public async Task<ActionResult<UserWithVehiclesDto>> GetUser(Guid userId)
    {
        try
        {
            var user = await _userService.GetUserAsync(userId);
            return Ok(user);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [AllowAnonymous]
    [HttpPost]
    public async Task<ActionResult<UserDto>> CreateUser(CreateUserDto dto)
    {
        if (_currentUserContext.IsAuthenticated && !_currentUserContext.IsAdmin)
        {
            return Forbid();
        }

        if (!_currentUserContext.IsAuthenticated)
        {
            dto.Role = UserRole.Customer;
        }

        try
        {
            var user = await _userService.CreateUserAsync(dto);
            return CreatedAtAction(nameof(GetUser), new { userId = user.UserId }, user);
        }
        catch (EmailAlreadyExistsException)
        {
            return Conflict(new { error = "Endereço de email já está em uso" });
        }
        catch (UserCreationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // Atenção aos verbos HTTP!   https://medium.com/@gabrielrufino.js/put-vs-patch-pare-de-agora-escolher-errado-533b8c6058d9
    // PUT -> Atualiza TODOS os campos da entidade
    // PATCH -> Atualização partical da entidade (ex: apenas o nome ou email)
    [Authorize]
    [HttpPatch("{userId}")]
    public async Task<IActionResult> UpdateUser(Guid userId, UpdateUserDto dto)
    {
        if (!_currentUserContext.IsAdmin)
        {
            if (_currentUserContext.GetRequiredUserId() != userId)
            {
                return Forbid();
            }

            dto.Role = null;
        }

        try
        {
            var updateUser = await _userService.UpdateUserAsync(userId, dto);
            return Ok(updateUser);
        }

        catch (EmailAlreadyExistsException)
        {
            return Conflict(new { error = "Endereço de email já está em uso" });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{userId}")]
    public async Task<IActionResult> DeleteUser(Guid userId)
    {
        try
        {
            await _userService.DeleteUserAsync(userId);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }


}
