namespace Backend.Features.ParkingSettings;

public class ParkingSettings
{
    public Guid ParkingSettingsId { get; set; } = Guid.NewGuid();
    public int ToleranceMinutes { get; set; } = 15;
    public decimal BasePrice { get; set; } = 15.00m;
    public decimal HourlyRate { get; set; } = 5.00m;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}