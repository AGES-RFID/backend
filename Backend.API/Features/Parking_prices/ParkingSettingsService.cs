using Backend.Database;
using Microsoft.EntityFrameworkCore;

namespace Backend.Features.ParkingSettings;

public class ParkingSettingsNotFoundException : Exception
{
    public ParkingSettingsNotFoundException() : base("Configurações de estacionamento não encontradas") { }
}

public interface IParkingSettingsService
{
    Task<ParkingSettingsDto> GetSettingsAsync();
    Task<ParkingSettingsDto> UpdateSettingsAsync(UpdateParkingSettingsDto dto);
}

public class ParkingSettingsService(AppDbContext db) : IParkingSettingsService
{
    private readonly AppDbContext _db = db;

    public async Task<ParkingSettingsDto> GetSettingsAsync()
    {
        var settings = await _db.ParkingSettings.FirstOrDefaultAsync()
            ?? throw new ParkingSettingsNotFoundException();

        return ParkingSettingsDto.FromModel(settings);
    }

    public async Task<ParkingSettingsDto> UpdateSettingsAsync(UpdateParkingSettingsDto dto)
    {
        var settings = await _db.ParkingSettings.FirstOrDefaultAsync()
            ?? throw new ParkingSettingsNotFoundException();

        settings.ToleranceMinutes = dto.ToleranceMinutes ?? settings.ToleranceMinutes;
        settings.BasePrice = dto.BasePrice ?? settings.BasePrice;
        settings.HourlyRate = dto.HourlyRate ?? settings.HourlyRate;

        await _db.SaveChangesAsync();

        return ParkingSettingsDto.FromModel(settings);
    }
}