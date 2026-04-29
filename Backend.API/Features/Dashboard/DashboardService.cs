using Backend.Database;
using Backend.Features.Accesses;
using Backend.Features.Vehicles;
using Microsoft.EntityFrameworkCore;

namespace Backend.Features.Dashboard;

public interface IDashboardService
{
    Task<OccupancyDto> GetOccupancyAsync();
}

public class DashboardService(AppDbContext db) : IDashboardService
{
    private readonly AppDbContext _db = db;

    public async Task<OccupancyDto> GetOccupancyAsync()
    {
        var tagIdsInside = await _db.Accesses
            .AsNoTracking()
            .GroupBy(a => a.TagId)
            .Select(g => new
            {
                TagId = g.Key,
                LastType = g.OrderByDescending(a => a.Timestamp).First().Type
            })
            .Where(x => x.LastType == AccessType.ENTRY)
            .Select(x => x.TagId)
            .ToListAsync();

        var vehicles = await _db.Vehicles
            .AsNoTracking()
            .Include(v => v.User)
            .Where(v => v.TagId != null && tagIdsInside.Contains(v.TagId))
            .ToListAsync();

        var vehicleDtos = vehicles.Select(VehicleDto.FromModel).ToList();

        return new OccupancyDto
        {
            CurrentOccupancy = vehicleDtos.Count,
            Vehicles = vehicleDtos
        };
    }
}