using System.ComponentModel.DataAnnotations;
namespace Backend.Features.Vehicles;

public class CreateVehicleDto
{
    [Required]
    public required Guid UserId { get; set; }

    [Required]
    [MinLength(1)]
    public required string Model { get; set; }

    [Required]
    [MinLength(1)]
    public required string Brand { get; set; }

    [Required]
    [MinLength(1)]
    [MaxLength(7)]
    public required string Plate { get; set; }
}
