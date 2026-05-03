using Backend.Database;
using Backend.Features.Accesses.Dtos;
using Backend.Features.Tags;
using Microsoft.EntityFrameworkCore;

namespace Backend.Features.Accesses;

public class AccessService(AppDbContext context) : IAccessService
{
    private readonly AppDbContext _context = context;

    public async Task<AccessDto> CreateAccessAsync(CreateAccessDto dto)
    {
        var tag = await _context.Tags.FirstOrDefaultAsync(t => t.Epc == dto.Epc);
        
        if (tag == null)
        {
            tag = new Tag
            {
                TagId = dto.Tid,
                Epc = dto.Epc
            };
            _context.Tags.Add(tag);
            await _context.SaveChangesAsync();
        }

        var accessType = dto.Entrance ? AccessType.Entry : AccessType.Exit;
        
        var access = new Access
        {
            TagId = tag.TagId,
            Type = accessType,
            Tag = tag,
            Timestamp = dto.Timestamp.ToUniversalTime()
        };

        _context.Accesses.Add(access);
        await _context.SaveChangesAsync();

        return new AccessDto
        {
            AccessId = access.AccessId,
            TagId = access.TagId,
            Epc = tag.Epc,
            Type = accessType.ToString(),
            Timestamp = access.Timestamp
        };
    }

    public async Task<IEnumerable<AccessDto>> GetAllAccessesAsync()
    {
        var accesses = await _context.Accesses
            .Include(a => a.Tag)
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync();

        return accesses.Select(access => new AccessDto
        {
            AccessId = access.AccessId,
            TagId = access.TagId,
            Epc = access.Tag.Epc,
            Type = access.Type.ToString(),
            Timestamp = access.Timestamp
        });
    }
}
