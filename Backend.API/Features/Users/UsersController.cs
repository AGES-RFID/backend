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

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<UserWithVehiclesDto>>> GetAllUsers()
    {
        var users = await _userService.GetAllUsersAsync();
        return Ok(users);
    }

    [HttpGet("by-name/{name}")]
    [Authorize(Roles = "Admin")]
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

    [HttpGet("{userId}")]
    [Authorize(Roles = "Admin")]
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

    [HttpPost]
    [AllowAnonymous]
    public async Task<ActionResult<UserDto>> CreateUser(CreateUserDto dto)
    {
        try
        {
            if (_currentUserContext.IsAuthenticated && !_currentUserContext.IsAdmin)
                return Forbid();

            var createDto = dto;
            if (!_currentUserContext.IsAuthenticated)
            {
                createDto = new CreateUserDto
                {
                    Name = dto.Name,
                    Email = dto.Email,
                    Password = dto.Password,
                    Cpf = dto.Cpf,
                    Cellphone = dto.Cellphone,
                    Role = UserRole.Customer
                };
            }

            var user = await _userService.CreateUserAsync(createDto);
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
    [HttpPatch("{userId}")]
    [Authorize]
    public async Task<IActionResult> UpdateUser(Guid userId, UpdateUserDto dto)
    {
        try
        {
            var actorUserId = _currentUserContext.GetRequiredUserId();
            var actorRole = _currentUserContext.GetRequiredRole();

            if (actorRole != UserRole.Admin && actorUserId != userId)
                return Forbid();

            if (actorRole != UserRole.Admin)
                dto.Role = null;

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

    [HttpDelete("{userId}")]
    [Authorize(Roles = "Admin")]
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
