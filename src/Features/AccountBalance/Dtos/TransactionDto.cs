using Backend.Features.AccountBalance.Enums;

namespace Backend.Features.AccountBalance.Dtos;

public class TransactionDto
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? Description { get; set; }
    public int? VehicleId { get; set; }
    public string? VehiclePlate { get; set; }
}
