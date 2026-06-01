using System.ComponentModel.DataAnnotations;

namespace Backend.Features.ParkingPrices;

public class UpdateParkingPriceDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Tolerance minutes must be greater than 0")]
    public int? ToleranceMinutes { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Base price must be greater than 0")]
    public decimal? BasePrice { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Hourly rate must be greater than 0")]
    public decimal? HourlyRate { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Threshold minutes must be greater than 0")]
    public int? ThresholdMinutes { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Max occupancy must be greater than 0")]
    public int? MaxOccupancy { get; set; }
}
