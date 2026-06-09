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
        var accessesQuery = _db.Accesses.AsNoTracking();

        if (accessType.HasValue)
        {
            accessesQuery = accessesQuery.Where(a => a.Type == accessType.Value);
        }

        var query =
            from access in accessesQuery
            join tag in _db.Tags.AsNoTracking() on access.TagId equals tag.TagId
            join vehicle in _db.Vehicles.AsNoTracking() on tag.TagId equals vehicle.TagId
            where !string.IsNullOrWhiteSpace(vehicle.Plate)
            orderby access.Timestamp descending
            select new AccessDto
            {
                AccessId = access.AccessId,
                TagId = access.TagId,
                Type = access.Type,
                Timestamp = access.Timestamp,
                Plate = vehicle.Plate,
                Value = _db.Transactions
                    .Where(t => t.AccessId == access.AccessId)
                    .OrderByDescending(t => t.CreatedAt)
                    .Select(t => (decimal?)t.Amount)
                    .FirstOrDefault()
            };

        return await query.ToListAsync();
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

    public async Task<TimeseriesResponseDto> GetTimeSeriesAsync()
    {
        var to = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Hour, 0, 0, DateTimeKind.Utc);
        var from = to.AddHours(-24);

        var accesses = await _db.Accesses
            .AsNoTracking()
            .Where(a => a.Timestamp >= from && a.Timestamp < to)
            .Select(a => new { a.Timestamp, a.Type })
            .ToListAsync();

        var entryPoints = new List<TimeSeriesPointDto>();
        var exitPoints = new List<TimeSeriesPointDto>();

        for (int i = 23; i >= 0; i--)
        {
            var time = to.AddHours(-i - 1);
            var nextTime = time.AddHours(1);

            int entryCount = 0;
            int exitCount = 0;

            foreach (var a in accesses)
            {
                foreach (var b in accesses)
                {
                    if (a.Equals(b) && a.Timestamp >= time && a.Timestamp < nextTime)
                    {
                        if (a.Type == AccessType.Entry) entryCount++;
                        if (a.Type == AccessType.Exit) exitCount++;
                    }
                }
            }

            entryPoints.Add(new TimeSeriesPointDto
            {
                Timestamp = time,
                Count = entryCount
            });

            exitPoints.Add(new TimeSeriesPointDto
            {
                Timestamp = time,
                Count = exitCount
            });
        }

        return new TimeseriesResponseDto
        {
            From = from,
            To = to,
            Series = new List<TimeSeriesDto>
            {
                new TimeSeriesDto
                {
                    Key = "entries",
                    Points = entryPoints
                },
                new TimeSeriesDto
                {
                    Key = "exits",
                    Points = exitPoints
                }
            }
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
