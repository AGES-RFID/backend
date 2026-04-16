namespace Backend.Features.Tags;

using System.ComponentModel.DataAnnotations;

public class AssignVehicleDto
{
    [Required(ErrorMessage = "VehicleId is required")]
    public required Guid VehicleId { get; set; }
}
