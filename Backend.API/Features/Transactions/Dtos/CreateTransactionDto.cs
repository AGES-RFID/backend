using System.ComponentModel.DataAnnotations;
using Backend.Features.Transactions;

public class CreateTransactionDto
{
    [Required]
    public required Guid UserId { get; set; }
    
    [MinLength(1)]
    public required string Description { get; set; }
        
    [Required]
    [Range(0.01, double.MaxValue)]
    public required decimal Amount { get; set; }
}