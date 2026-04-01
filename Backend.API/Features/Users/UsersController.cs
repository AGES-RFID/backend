using Microsoft.AspNetCore.Mvc;

namespace Backend.Features.Users;

[ApiController]
[Route("api/users")]
public class UsersController(IUserService userService) : ControllerBase
{
    private readonly IUserService _userService = userService;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetAllUsers()
    {
        var users = await _userService.GetAllUsersAsync();
        return Ok(users);
    }

    [HttpGet("{userId}")]
    public async Task<ActionResult<UserDto>> GetUser(Guid userId)
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
    public async Task<ActionResult<UserDto>> CreateUser(CreateUserDto dto)
    {
        var user = await _userService.CreateUserAsync(dto);
        return CreatedAtAction(nameof(GetUser), new { userId = user.UserId }, user);
    }

    // Atenção aos verbos HTTP!   https://medium.com/@gabrielrufino.js/put-vs-patch-pare-de-agora-escolher-errado-533b8c6058d9
    // PUT -> Atualiza TODOS os campos da entidade
    // PATCH -> Atualização partical da entidade (ex: apenas o nome ou email)
    [HttpPut("{userId}")]
    public async Task<IActionResult> UpdateUser(Guid userId, CreateUserDto dto)
    {
        try
        {
            await _userService.UpdateUserAsync(userId, dto);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

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
