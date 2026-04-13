namespace Backend.Features.Vehicles;

public class VehicleSearchResponseDto
{
    public required Guid VehicleId { get; set; }
    public required string OwnerName { get; set; }
    public required string Plate { get; set; }
}
