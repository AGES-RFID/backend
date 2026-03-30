using Backend.Features.Common.Mapping;

namespace Backend.Features.Admins;

public interface IAdminService
{
    Task<AdminDto> CreateAdminAsync(CreateAdminDto dto);
    Task<AdminDto> GetAdminAsync(int id);
    Task<AdminDto> UpdateAdminAsync(int id, CreateAdminDto dto);
    Task<IEnumerable<AdminDto>> GetAllAdminsAsync();
}

public class AdminService : IAdminService
{
    private readonly List<Backend.Features.Admins.Models.Admin> _admins = new();

    public async Task<AdminDto> CreateAdminAsync(CreateAdminDto dto)
    {
        var admin = dto.ToEntity();
        admin.UserId = _admins.Count + 1;

        _admins.Add(admin);

        return admin.ToDto();
    }

    public async Task<AdminDto> GetAdminAsync(int id)
    {
        var admin = _admins.FirstOrDefault(a => a.UserId == id);
        if (admin == null)
            throw new KeyNotFoundException($"Administrador com ID {id} não encontrado");

        return admin.ToDto();
    }

    public async Task<AdminDto> UpdateAdminAsync(int id, CreateAdminDto dto)
    {
        var admin = _admins.FirstOrDefault(a => a.UserId == id);
        if (admin == null)
            throw new KeyNotFoundException($"Administrador com ID {id} não encontrado");

        admin.UpdateEntity(dto);

        return admin.ToDto();
    }

    public async Task<IEnumerable<AdminDto>> GetAllAdminsAsync()
    {
        return _admins.ToDtos();
    }
}
