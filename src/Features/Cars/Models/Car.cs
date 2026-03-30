namespace Backend.Features.Cars.Models;

public class Car
{
    public int CarId { get; set; }
    public required string PlateNumber { get; set; }
    public int CustomerId { get; set; }
    public int? RfidTagId { get; set; }
    public DateTime? LastEntry { get; set; }
    public DateTime? LastExit { get; set; }
    public TimeSpan? DurationInParkingLot { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
