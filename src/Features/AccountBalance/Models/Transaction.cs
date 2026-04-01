using Backend.Features.AccountBalance.Enums;

namespace Backend.Features.AccountBalance.Models;

public class Transaction
{
    public int TransactionId { get; set; }
    public int CustomerId { get; set; }
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? Description { get; set; }
    public int? VehicleId { get; set; }
    public string? VehiclePlate { get; set; }
}
