using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Backend.Features.Auth;
using Backend.Features.Users;
using Microsoft.AspNetCore.Http;

namespace Backend.Tests.Unit.Features.Auth;

public class CurrentUserContextTests
{
    private static ICurrentUserContext CreateSut(params Claim[] claims)
    {
        var httpContext = new DefaultHttpContext();
        var identity = claims.Length > 0
            ? new ClaimsIdentity(claims, authenticationType: "Bearer")
            : new ClaimsIdentity();

        httpContext.User = new ClaimsPrincipal(identity);

        var accessor = new HttpContextAccessor { HttpContext = httpContext };
        return new CurrentUserContext(accessor);
    }

    [Fact]
    public void IsAuthenticated_WhenNoIdentityAuthentication_ReturnsFalse()
    {
        var sut = CreateSut();

        Assert.False(sut.IsAuthenticated);
    }

    [Fact]
    public void UserId_WhenSubClaimIsValidGuid_ReturnsGuid()
    {
        var userId = Guid.NewGuid();
        var sut = CreateSut(new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()));

        Assert.Equal(userId, sut.UserId);
    }

    [Fact]
    public void UserId_WhenSubClaimIsInvalid_ReturnsNull()
    {
        var sut = CreateSut(new Claim(JwtRegisteredClaimNames.Sub, "not-a-guid"));

        Assert.Null(sut.UserId);
    }

    [Fact]
    public void Role_WhenRoleClaimIsPresent_ReturnsParsedEnum()
    {
        var sut = CreateSut(new Claim("role", "Admin"));

        Assert.Equal(UserRole.Admin, sut.Role);
    }

    [Fact]
    public void IsAdmin_WhenRoleIsAdmin_ReturnsTrue()
    {
        var sut = CreateSut(new Claim("role", "Admin"));

        Assert.True(sut.IsAdmin);
    }

    [Fact]
    public void IsAdmin_WhenRoleIsCustomer_ReturnsFalse()
    {
        var sut = CreateSut(new Claim("role", "Customer"));

        Assert.False(sut.IsAdmin);
    }

    [Fact]
    public void GetRequiredUserId_WhenMissingSubClaim_ThrowsUnauthorizedAccessException()
    {
        var sut = CreateSut(new Claim("role", "Admin"));

        Assert.Throws<UnauthorizedAccessException>(() => sut.GetRequiredUserId());
    }

    [Fact]
    public void GetRequiredRole_WhenMissingRoleClaim_ThrowsUnauthorizedAccessException()
    {
        var sut = CreateSut(new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()));

        Assert.Throws<UnauthorizedAccessException>(() => sut.GetRequiredRole());
    }
}
