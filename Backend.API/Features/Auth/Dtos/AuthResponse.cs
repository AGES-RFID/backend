using Backend.Features.Users;

namespace Backend.Features.Auth;

public class AuthResponse
{
    public required string Token { get; set; }
    public required UserDto User { get; set; }
}
