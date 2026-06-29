using Backend.Database;
using Backend.Features.Tags;
using Backend.Features.Tags.Enums;
using Microsoft.EntityFrameworkCore;

namespace Backend.Features.Accesses;

public interface IAccessesService
{
    Task<IEnumerable<AccessDto>> GetAccessesAsync(AccessType? accessType = null);
    Task<AccessDto> RegisterAccessAsync(CreateAccessDto dto);
    Task<TimeseriesResponseDto> GetTimeSeriesAsync();
}

public class AccessesService(AppDbContext db) : IAccessesService
{
    private readonly AppDbContext _db = db;

    public async Task<IEnumerable<AccessDto>> GetAccessesAsync(AccessType? accessType = null)
    {
        var query = _db.Accesses.AsNoTracking();

        if (accessType.HasValue)
        {
            query = query.Where(a => a.Type == accessType.Value);
        }

        return await query
            .Where(a => a.Tag.Vehicle != null && !string.IsNullOrWhiteSpace(a.Tag.Vehicle.Plate))
            .OrderByDescending(a => a.Timestamp)
            .Select(a => new AccessDto
            {
                AccessId = a.AccessId,
                TagId = a.TagId,
                Type = a.Type,
                Timestamp = a.Timestamp,
                Plate = a.Tag.Vehicle!.Plate,
                Value = _db.Transactions
                    .Where(t => t.AccessId == a.AccessId)
                    .OrderByDescending(t => t.CreatedAt)
                    .Select(t => (decimal?)t.Amount)
                    .FirstOrDefault()
            })
            .ToListAsync();
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
                throw new AccessRegistrationConflictException(
                    "tag_already_inside",
                    "Access registration failed because this tag is already inside the parking lot.",
                    "The tag is already inside the parking lot. Entry was not registered.");
        }
        else
        {
            if (lastAccess == null || lastAccess.Type == AccessType.Exit)
                throw new AccessRegistrationConflictException(
                    "tag_already_outside",
                    "Access registration failed because this tag is already outside the parking lot.",
                    "The tag is already outside the parking lot. Exit was not registered.");
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

    public async Task<TimeseriesResponseDto> GetTimeSeriesAsync()
    {
        var now = DateTime.UtcNow;
        var to = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, DateTimeKind.Utc);
        var from = to.AddHours(-24);

        var accesses = await _db.Accesses
            .AsNoTracking()
            .Where(a => a.Timestamp >= from && a.Timestamp < to)
            .Select(a => new { a.Timestamp, a.Type })
            .ToListAsync();

        var lookup = accesses.ToLookup(
            a => new DateTime(a.Timestamp.Year, a.Timestamp.Month, a.Timestamp.Day, a.Timestamp.Hour, 0, 0, DateTimeKind.Utc),
            a => a.Type
        );

        var times = Enumerable.Range(0, 24).Select(i => from.AddHours(i)).ToArray();

        return new TimeseriesResponseDto
        {
            From = from,
            To = to,
            Series = [
                new TimeSeriesDto {
                    Key = "entries",
                    Points = times.Select(t => new TimeSeriesPointDto { Timestamp = t, Count = lookup[t].Count(x => x == AccessType.Entry) })
                },
                new TimeSeriesDto {
                    Key = "exits",
                    Points = times.Select(t => new TimeSeriesPointDto { Timestamp = t, Count = lookup[t].Count(x => x == AccessType.Exit) })
                }
            ]
        };
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
