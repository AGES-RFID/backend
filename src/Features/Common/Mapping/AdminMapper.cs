using Backend.Features.Admins.Dtos;
using Backend.Features.Admins.Models;

namespace Backend.Features.Common.Mapping;

public static class AdminMapper
{
    public static AdminDto ToDto(this Admin admin)
    {
        return new AdminDto
        {
            Id = admin.UserId,
            Email = admin.Email,
            Name = admin.Name,
            CreatedAt = admin.CreatedAt,
            UpdatedAt = admin.UpdatedAt
        };
    }

    public static Admin ToEntity(this CreateAdminDto dto)
    {
        return new Admin
        {
            Email = dto.Email,
            Name = dto.Name
        };
    }

    public static void UpdateEntity(this Admin admin, CreateAdminDto dto)
    {
        admin.Email = dto.Email;
        admin.Name = dto.Name;
        admin.UpdatedAt = DateTime.UtcNow;
    }

    public static IEnumerable<AdminDto> ToDtos(this IEnumerable<Admin> admins)
    {
        return admins.Select(admin => admin.ToDto());
    }
}
