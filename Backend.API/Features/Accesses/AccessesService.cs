using Backend.Database;
using Backend.Features.Tags.Enums;
using Microsoft.EntityFrameworkCore;

namespace Backend.Features.Accesses;

public interface IAccessesService
{
    Task<AccessDto> RegisterEntryAsync(CreateAccessDto dto);
    Task<AccessDto> RegisterExitAsync(CreateAccessDto dto);
}

public class AccessesService(AppDbContext db) : IAccessesService
{
    private readonly AppDbContext _db = db;

    public async Task<AccessDto> RegisterEntryAsync(CreateAccessDto dto)
    {

        var tag = await GetActiveTagAsync(dto.TagId);


        var lastAccess = await _db.Accesses
            .Where(a => a.TagId == dto.TagId)
            .OrderByDescending(a => a.Timestamp)
            .FirstOrDefaultAsync();

        if (lastAccess != null && lastAccess.Type == AccessType.Entry)
            throw new InvalidOperationException("Não é possível registrar a entrada: o veículo já está no estacionamento.");


        var access = new Access
        {
            TagId = dto.TagId,
            Type = AccessType.Entry,
            Tag = tag,
            Timestamp = DateTime.UtcNow
        };

        await _db.Accesses.AddAsync(access);
        await _db.SaveChangesAsync();

        return AccessDto.FromModel(access);
    }

    public async Task<AccessDto> RegisterExitAsync(CreateAccessDto dto)
    {

        var tag = await GetActiveTagAsync(dto.TagId);


        var lastAccess = await _db.Accesses
            .Where(a => a.TagId == dto.TagId)
            .OrderByDescending(a => a.Timestamp)
            .FirstOrDefaultAsync();

        if (lastAccess == null || lastAccess.Type == AccessType.Exit)
            throw new InvalidOperationException("Não é possível registrar a saída: o veículo não está no estacionamento.");


        var access = new Access
        {
            TagId = dto.TagId,
            Type = AccessType.Exit,
            Tag = tag,
            Timestamp = DateTime.UtcNow
        };

        await _db.Accesses.AddAsync(access);
        await _db.SaveChangesAsync();

        return AccessDto.FromModel(access);
    }

    private async Task<Tag> GetActiveTagAsync(string tagId)
    {
        var tag = await _db.Tags
            .Include(t => t.Vehicle)
            .FirstOrDefaultAsync(t => t.TagId == tagId)
            ?? throw new KeyNotFoundException("A tag informada não existe.");

        if (tag.Status != TagStatus.IN_USE)
            throw new InvalidOperationException("A tag informada não está ativa para acesso.");

        if (tag.Vehicle == null)
            throw new KeyNotFoundException("A tag informada não está vinculada a um veículo.");

        return tag;
    }
}
