using Backend.Features.Common.Mapping;
using Backend.Features.Common.Services;

namespace Backend.Features.Admins;

public interface IAdminService
{
    Task<AdminDto> CreateAdminAsync(CreateAdminDto dto);
    Task<AdminDto> GetAdminAsync(int id);
    Task<AdminDto> UpdateAdminAsync(int id, CreateAdminDto dto);
    Task<IEnumerable<AdminDto>> GetAllAdminsAsync();
}

public class AdminService : BaseService, IAdminService
{
    private readonly List<Backend.Features.Admins.Models.Admin> _admins = new();

    public async Task<AdminDto> CreateAdminAsync(CreateAdminDto dto)
    {
        var admin = dto.ToEntity();
        admin.UserId = GenerateId(_admins);

        _admins.Add(admin);

        return admin.ToDto();
    }

    public async Task<AdminDto> GetAdminAsync(int id)
    {
        var admin = _admins.FirstOrDefault(a => a.UserId == id);
        ValidateNotNull(admin, "Administrador", id);

        return admin.ToDto();
    }

    public async Task<AdminDto> UpdateAdminAsync(int id, CreateAdminDto dto)
    {
        var admin = _admins.FirstOrDefault(a => a.UserId == id);
        ValidateNotNull(admin, "Administrador", id);

        admin.UpdateEntity(dto);

        return admin.ToDto();
    }

    public async Task<IEnumerable<AdminDto>> GetAllAdminsAsync()
    {
        return _admins.ToDtos();
    }
}
