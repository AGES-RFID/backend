using System.ComponentModel.DataAnnotations;

namespace Backend.Features.ParkingSettings;

public class UpdateParkingSettingsDto
{
    [Range(0, int.MaxValue)]
    public int? ToleranceMinutes { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? BasePrice { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? HourlyRate { get; set; }
}