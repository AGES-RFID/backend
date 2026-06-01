using System.ComponentModel.DataAnnotations;

namespace Backend.Features.ParkingPrices;

public class CreateParkingPriceDto
{
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Tolerance minutes must be greater than 0")]
    public required int ToleranceMinutes { get; set; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Base price must be greater than 0")]
    public required decimal BasePrice { get; set; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Hourly rate must be greater than 0")]
    public required decimal HourlyRate { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Threshold minutes must be greater than 0")]
    public required int ThresholdMinutes { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Max occupancy must be greater than 0")]
    public required int MaxOccupancy { get; set; }
}
