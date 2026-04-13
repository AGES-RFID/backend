using Backend.Database;
using Microsoft.EntityFrameworkCore;
namespace Backend.Features.Vehicles;

public interface IVehicleService
{
    Task<VehicleDto> GetVehicleAsync(Guid vehicleId);
    Task<IEnumerable<VehicleDto>> GetAllVehiclesAsync();
    Task<VehicleDto> CreateVehicleAsync(CreateVehicleDto dto);
    Task<VehicleDto> UpdateVehicleAsync(Guid id, CreateVehicleDto dto);
    Task DeleteVehicleAsync(Guid id);
    Task<VehicleSearchResponseDto> GetVehicleByPlateAsync(string plate);
}

public class VehicleService(AppDbContext db) : IVehicleService
{
    private readonly AppDbContext _db = db;

    public async Task<VehicleDto> CreateVehicleAsync(CreateVehicleDto dto)
    {
        var existsUser = await _db.Users.AnyAsync(u => u.UserId == dto.UserId);
        if (!existsUser)
        {
            throw new KeyNotFoundException("User not found");
        }
        var exists = await _db.Vehicles.AnyAsync(v => v.Plate == dto.Plate);
        if (exists)
        {
            throw new InvalidOperationException("Plate already exists. Try again.");
        }

        var vehicle = await _db.Vehicles.AddAsync(new Vehicle
        {
            Plate = dto.Plate,
            Brand = dto.Brand,
            Model = dto.Model,
            UserId = dto.UserId
        });

        await _db.SaveChangesAsync();
        return VehicleDto.FromModel(vehicle.Entity);
    }

    public async Task<IEnumerable<VehicleDto>> GetAllVehiclesAsync()
    {
        var vehicles = await _db.Vehicles.AsNoTracking()
            .Select(v => VehicleDto.FromModel(v))
            .ToListAsync();

        return vehicles;
    }

    public async Task<VehicleDto> GetVehicleAsync(Guid vehicleId)
    {
        var vehicle = await _db.Vehicles.FirstOrDefaultAsync(v => v.VehicleId == vehicleId)
            ?? throw new KeyNotFoundException($"Vehicle with id {vehicleId} not found");

        return VehicleDto.FromModel(vehicle);
    }

    public async Task<VehicleSearchResponseDto> GetVehicleByPlateAsync(string plate)
    {
        var vehicle = await _db.Vehicles
            .Include(v => v.User)
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Plate == plate)
            ?? throw new KeyNotFoundException("Veículo não encontrado.");

        return new VehicleSearchResponseDto
        {
            VehicleId = vehicle.VehicleId,
            OwnerName = vehicle.User!.Name,
            Plate = vehicle.Plate
        };
    }

    public async Task<VehicleDto> UpdateVehicleAsync(Guid id, CreateVehicleDto dto)
    {
        var vehicle = await _db.Vehicles.FirstOrDefaultAsync(v => v.VehicleId == id)
            ?? throw new KeyNotFoundException($"Vehicle with id {id} not found");

        var exists = await _db.Vehicles.AnyAsync(v => v.Plate == dto.Plate && v.VehicleId != id);
        if (exists)
        {
            throw new InvalidOperationException("Plate already exists. Try again.");
        }

        vehicle.Plate = dto.Plate;
        vehicle.Brand = dto.Brand;
        vehicle.Model = dto.Model;
        vehicle.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return VehicleDto.FromModel(vehicle);
    }

    public async Task DeleteVehicleAsync(Guid id)
    {
        var vehicle = await _db.Vehicles.FirstOrDefaultAsync(v => v.VehicleId == id)
            ?? throw new KeyNotFoundException($"Vehicle with id {id} not found");

        _db.Vehicles.Remove(vehicle);
        await _db.SaveChangesAsync();
    }
}
