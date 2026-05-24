using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Backend.Features.Users;

namespace Backend.Features.Auth;

public interface ICurrentUserContext
{
    bool IsAuthenticated { get; }
    Guid? UserId { get; }
    UserRole? Role { get; }
    bool IsAdmin { get; }

    Guid GetRequiredUserId();
    UserRole GetRequiredRole();
}

public class CurrentUserContext(IHttpContextAccessor httpContextAccessor) : ICurrentUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public Guid? UserId
    {
        get
        {
            var sub = Principal?.FindFirstValue(JwtRegisteredClaimNames.Sub);
            return Guid.TryParse(sub, out var userId) ? userId : null;
        }
    }

    public UserRole? Role
    {
        get
        {
            var role = Principal?.FindFirstValue("role");
            return Enum.TryParse<UserRole>(role, ignoreCase: true, out var parsed) ? parsed : null;
        }
    }

    public bool IsAdmin => Role == UserRole.Admin;

    public Guid GetRequiredUserId()
        => UserId ?? throw new UnauthorizedAccessException("Token inválido: claim 'sub' ausente ou inválida.");

    public UserRole GetRequiredRole()
        => Role ?? throw new UnauthorizedAccessException("Token inválido: claim 'role' ausente ou inválida.");
}
