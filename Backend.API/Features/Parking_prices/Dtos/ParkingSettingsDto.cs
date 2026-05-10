namespace Backend.Features.ParkingSettings;

public class ParkingSettingsDto
{
    public Guid ParkingSettingsId { get; set; }
    public int ToleranceMinutes { get; set; }
    public decimal BasePrice { get; set; }
    public decimal HourlyRate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public static ParkingSettingsDto FromModel(ParkingSettings settings) => new()
    {
        ParkingSettingsId = settings.ParkingSettingsId,
        ToleranceMinutes = settings.ToleranceMinutes,
        BasePrice = settings.BasePrice,
        HourlyRate = settings.HourlyRate,
        CreatedAt = settings.CreatedAt,
        UpdatedAt = settings.UpdatedAt
    };
}