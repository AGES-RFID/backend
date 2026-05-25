using Backend.Database;
using Backend.Features.Tags;
using Backend.Features.Tags.Enums;
using Microsoft.EntityFrameworkCore;

namespace Backend.Features.Accesses;

public interface IAccessesService
{
    Task<IEnumerable<AccessDto>> GetAccessesAsync();
    Task<AccessDto> RegisterAccessAsync(CreateAccessDto dto);
}

public class AccessesService(AppDbContext db) : IAccessesService
{
    private readonly AppDbContext _db = db;

    public async Task<IEnumerable<AccessDto>> GetAccessesAsync()
    {
        var accesses = await _db.Accesses
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync();

        return accesses.Select(AccessDto.FromModel);
    }

    public async Task<AccessDto> RegisterAccessAsync(CreateAccessDto dto)
    {
        var tag = await GetActiveTagAsync(dto.Tid, dto.Epc);

        var lastAccess = await _db.Accesses
            .Where(a => a.TagId == tag.TagId)
            .OrderByDescending(a => a.Timestamp)
            .FirstOrDefaultAsync();

        if (dto.Entrance)
        {
            if (lastAccess != null && lastAccess.Type == AccessType.Entry)
                throw new InvalidOperationException("Não é possível registrar a entrada: o veículo já está no estacionamento.");
        }
        else
        {
            if (lastAccess == null || lastAccess.Type == AccessType.Exit)
                throw new InvalidOperationException("Não é possível registrar a saída: o veículo não está no estacionamento.");
        }

        var access = new Access
        {
            TagId = tag.TagId,
            Type = dto.Entrance ? AccessType.Entry : AccessType.Exit,
            Tag = tag,
            Timestamp = DateTime.UtcNow
        };

        await _db.Accesses.AddAsync(access);
        await _db.SaveChangesAsync();

        return AccessDto.FromModel(access);
    }

    private async Task<Tag> GetActiveTagAsync(string tid, string epc)
    {
        var tag = await _db.Tags
            .Include(t => t.Vehicle)
            .FirstOrDefaultAsync(t => t.Tid == tid && t.Epc == epc)
            ?? throw new KeyNotFoundException("A tag informada não existe.");

        if (tag.Status != TagStatus.IN_USE)
            throw new InvalidOperationException("A tag informada não está ativa para acesso.");

        if (tag.Vehicle == null)
            throw new KeyNotFoundException("A tag informada não está vinculada a um veículo.");

        return tag;
    }
}
