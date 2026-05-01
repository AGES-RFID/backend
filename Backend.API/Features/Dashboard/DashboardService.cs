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
        var vehicles = await _db.Vehicles
            .AsNoTracking()
            .Include(v => v.User)
            .Where(v => v.TagId != null && _db.Accesses
                .Where(a => a.TagId == v.TagId)
                .OrderByDescending(a => a.Timestamp)
                .Select(a => a.Type)
                .FirstOrDefault() == AccessType.ENTRY)
            .ToListAsync();

        var vehicleDtos = vehicles.Select(VehicleDto.FromModel).ToList();

        return new OccupancyDto
        {
            CurrentOccupancy = vehicleDtos.Count,
            Vehicles = vehicleDtos
        };
    }
}