namespace Backend.Features.Cars.Dtos;

public class CarDto
{
    public int Id { get; set; }
    public string PlateNumber { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public int? RfidTagId { get; set; }
    public DateTime? LastEntry { get; set; }
    public DateTime? LastExit { get; set; }
    public TimeSpan? DurationInParkingLot { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
