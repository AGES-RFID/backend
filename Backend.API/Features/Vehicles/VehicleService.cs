using Backend.Database;
using Backend.Features.Auth;
using Backend.Features.Users;
using Microsoft.EntityFrameworkCore;

namespace Backend.Features.Vehicles;

public interface IVehicleService
{
    Task<VehicleWithOwnerDto> GetVehicleAsync(Guid vehicleId);
    Task<IEnumerable<VehicleWithOwnerDto>> GetAllVehiclesAsync(bool includeUsers);
    Task<VehicleDto> CreateVehicleAsync(CreateVehicleDto dto);
    Task<VehicleDto> UpdateVehicleAsync(Guid id, CreateVehicleDto dto);
    Task DeleteVehicleAsync(Guid id);
    Task<VehicleSearchResponseDto> GetVehicleByPlateAsync(string plate);
}

public class VehicleService(AppDbContext db, ICurrentUserContext currentUserContext) : IVehicleService
{
    private readonly AppDbContext _db = db;
    private readonly ICurrentUserContext _currentUserContext = currentUserContext;

    public async Task<VehicleDto> CreateVehicleAsync(CreateVehicleDto dto)
    {
        var actorUserId = _currentUserContext.GetRequiredUserId();
        var actorRole = _currentUserContext.GetRequiredRole();

        if (actorRole != UserRole.Admin && dto.UserId.HasValue && dto.UserId.Value != actorUserId)
            throw new KeyNotFoundException("User not found");

        var targetUserId = actorRole == UserRole.Admin
            ? dto.UserId ?? actorUserId
            : actorUserId;

        var existsUser = await _db.Users.AnyAsync(u => u.UserId == targetUserId);
        if (!existsUser)
            throw new KeyNotFoundException("User not found");

        var exists = await _db.Vehicles.AnyAsync(v => v.Plate == dto.Plate);
        if (exists)
            throw new InvalidOperationException("Plate already exists. Try again.");

        var vehicle = await _db.Vehicles.AddAsync(new Vehicle
        {
            Plate = dto.Plate,
            Brand = dto.Brand,
            Model = dto.Model,
            UserId = targetUserId
        });

        await _db.SaveChangesAsync();
        return VehicleDto.FromModel(vehicle.Entity);
    }

    public async Task<IEnumerable<VehicleWithOwnerDto>> GetAllVehiclesAsync(bool includeUsers)
    {
        var actorUserId = _currentUserContext.GetRequiredUserId();
        var actorRole = _currentUserContext.GetRequiredRole();

        var query = _db.Vehicles.AsNoTracking();

        if (actorRole != UserRole.Admin)
            query = query.Where(v => v.UserId == actorUserId);

        if (includeUsers)
            query = query.Include(v => v.User);

        var vehicles = await query
            .Select(v => VehicleWithOwnerDto.FromModel(v))
            .ToListAsync();

        return vehicles;
    }

    public async Task<VehicleWithOwnerDto> GetVehicleAsync(Guid vehicleId)
    {
        var actorUserId = _currentUserContext.GetRequiredUserId();
        var actorRole = _currentUserContext.GetRequiredRole();

        var vehicle = await _db.Vehicles
            .AsNoTracking()
            .Include(v => v.User)
            .FirstOrDefaultAsync(v => v.VehicleId == vehicleId)
            ?? throw new KeyNotFoundException($"Vehicle with id {vehicleId} not found");

        EnsureCanAccessVehicle(vehicle, actorUserId, actorRole);

        return VehicleWithOwnerDto.FromModel(vehicle);
    }

    public async Task<VehicleSearchResponseDto> GetVehicleByPlateAsync(string plate)
    {
        var actorUserId = _currentUserContext.GetRequiredUserId();
        var actorRole = _currentUserContext.GetRequiredRole();

        var vehicle = await _db.Vehicles
            .Include(v => v.User)
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Plate == plate)
            ?? throw new KeyNotFoundException("Veículo não encontrado.");

        EnsureCanAccessVehicle(vehicle, actorUserId, actorRole);

        return new VehicleSearchResponseDto
        {
            VehicleId = vehicle.VehicleId,
            OwnerName = vehicle.User!.Name,
            Plate = vehicle.Plate
        };
    }

    public async Task<VehicleDto> UpdateVehicleAsync(Guid id, CreateVehicleDto dto)
    {
        var actorUserId = _currentUserContext.GetRequiredUserId();
        var actorRole = _currentUserContext.GetRequiredRole();

        var vehicle = await _db.Vehicles.FirstOrDefaultAsync(v => v.VehicleId == id)
            ?? throw new KeyNotFoundException($"Vehicle with id {id} not found");

        EnsureCanAccessVehicle(vehicle, actorUserId, actorRole);

        if (actorRole != UserRole.Admin && dto.UserId.HasValue && dto.UserId.Value != actorUserId)
            throw new KeyNotFoundException("User not found");

        var targetUserId = actorRole == UserRole.Admin
            ? dto.UserId ?? vehicle.UserId
            : actorUserId;

        var existsUser = await _db.Users.AnyAsync(u => u.UserId == targetUserId);
        if (!existsUser)
            throw new KeyNotFoundException("User not found");

        var exists = await _db.Vehicles.AnyAsync(v => v.Plate == dto.Plate && v.VehicleId != id);
        if (exists)
            throw new InvalidOperationException("Plate already exists. Try again.");

        vehicle.UserId = targetUserId;
        vehicle.Plate = dto.Plate;
        vehicle.Brand = dto.Brand;
        vehicle.Model = dto.Model;
        vehicle.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return VehicleDto.FromModel(vehicle);
    }

    public async Task DeleteVehicleAsync(Guid id)
    {
        var actorUserId = _currentUserContext.GetRequiredUserId();
        var actorRole = _currentUserContext.GetRequiredRole();

        var vehicle = await _db.Vehicles.FirstOrDefaultAsync(v => v.VehicleId == id)
            ?? throw new KeyNotFoundException($"Vehicle with id {id} not found");

        EnsureCanAccessVehicle(vehicle, actorUserId, actorRole);

        _db.Vehicles.Remove(vehicle);
        await _db.SaveChangesAsync();
    }

    private static void EnsureCanAccessVehicle(Vehicle vehicle, Guid actorUserId, UserRole actorRole)
    {
        if (actorRole != UserRole.Admin && vehicle.UserId != actorUserId)
            throw new KeyNotFoundException("Vehicle not found");
    }
}
