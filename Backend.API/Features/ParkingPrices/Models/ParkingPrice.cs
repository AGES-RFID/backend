namespace Backend.Features.ParkingPrices;

public class ParkingPrice
{
    public Guid ParkingPriceId { get; set; } = Guid.NewGuid();
    public int ToleranceMinutes { get; set; } = 15;
    public decimal BasePrice { get; set; } = 15.00m;
    public decimal HourlyRate { get; set; } = 5.00m;
    public int ThresholdMinutes { get; set; } = 3 * 60;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
