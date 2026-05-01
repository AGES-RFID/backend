using Backend.Features.Accesses.Dtos;

namespace Backend.Features.Accesses;

public interface IAccessService
{
    Task<AccessDto> CreateAccessAsync(CreateAccessDto dto);
}
