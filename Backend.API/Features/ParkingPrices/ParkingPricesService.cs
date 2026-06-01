using Backend.Database;
using Microsoft.EntityFrameworkCore;

namespace Backend.Features.ParkingPrices;

public interface IParkingPricesService
{
    Task<ParkingPricesDto> GetParkingPriceAsync(Guid id);
    Task<IEnumerable<ParkingPricesDto>> GetAllParkingPricesAsync();
    Task<ParkingPricesDto> GetCurrentParkingPricingAsync();
    Task<ParkingPricesDto> CreateParkingPriceAsync(CreateParkingPriceDto dto);
    Task<ParkingPricesDto> UpdateParkingPriceAsync(Guid id, UpdateParkingPriceDto dto);
    Task DeleteParkingPriceAsync(Guid id);
}

public class ParkingPricesService(AppDbContext db) : IParkingPricesService
{
    private readonly AppDbContext _db = db;

    public async Task<ParkingPricesDto> GetParkingPriceAsync(Guid id)
    {
        var parkingPrice = await _db.ParkingPrices
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ParkingPriceId == id)
            ?? throw new KeyNotFoundException($"Parking price with id {id} was not found");

        return ParkingPricesDto.FromModel(parkingPrice);
    }

    public async Task<IEnumerable<ParkingPricesDto>> GetAllParkingPricesAsync()
    {
        var parkingPrices = await _db.ParkingPrices
            .AsNoTracking()
            .ToListAsync();

        return parkingPrices.Select(ParkingPricesDto.FromModel);
    }

    public async Task<ParkingPricesDto> GetCurrentParkingPricingAsync()
    {
        var parkingPrice = await _db.ParkingPrices
            .AsNoTracking()
            .OrderByDescending(p => p.UpdatedAt)
            .ThenByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException("Nenhuma regra de cobrança foi configurada.");

        return ParkingPricesDto.FromModel(parkingPrice);
    }

    public async Task<ParkingPricesDto> CreateParkingPriceAsync(CreateParkingPriceDto dto)
    {
        var parkingPrice = new ParkingPrice
        {
            ToleranceMinutes = dto.ToleranceMinutes,
            BasePrice = dto.BasePrice,
            MaxOccupancy = dto.MaxOccupancy,
            HourlyRate = dto.HourlyRate,
            ThresholdMinutes = dto.ThresholdMinutes
        };

        await _db.ParkingPrices.AddAsync(parkingPrice);
        await _db.SaveChangesAsync();

        return ParkingPricesDto.FromModel(parkingPrice);
    }

    public async Task<ParkingPricesDto> UpdateParkingPriceAsync(Guid id, UpdateParkingPriceDto dto)
    {
        var parkingPrice = await _db.ParkingPrices
            .FirstOrDefaultAsync(p => p.ParkingPriceId == id)
            ?? throw new KeyNotFoundException($"Parking price with id {id} was not found");

        parkingPrice.ToleranceMinutes = dto.ToleranceMinutes ?? parkingPrice.ToleranceMinutes;
        parkingPrice.BasePrice = dto.BasePrice ?? parkingPrice.BasePrice;
        parkingPrice.HourlyRate = dto.HourlyRate ?? parkingPrice.HourlyRate;
        parkingPrice.ThresholdMinutes = dto.ThresholdMinutes ?? parkingPrice.ThresholdMinutes;
        parkingPrice.MaxOccupancy = dto.MaxOccupancy ?? parkingPrice.MaxOccupancy;
        parkingPrice.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return ParkingPricesDto.FromModel(parkingPrice);
    }

    public async Task DeleteParkingPriceAsync(Guid id)
    {
        var parkingPrice = await _db.ParkingPrices
            .FirstOrDefaultAsync(p => p.ParkingPriceId == id)
            ?? throw new KeyNotFoundException($"Parking price with id {id} was not found");

        _db.ParkingPrices.Remove(parkingPrice);
        await _db.SaveChangesAsync();
    }
}
