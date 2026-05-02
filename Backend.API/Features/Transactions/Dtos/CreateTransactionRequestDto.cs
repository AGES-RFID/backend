using System.ComponentModel.DataAnnotations;

namespace Backend.Features.Transactions;

public class CreateTransactionRequestDto
{
    public Guid? UserId { get; set; }

    [MinLength(1)]
    public required string Description { get; set; }

    [Required]
    [Range(0.01, double.MaxValue)]
    public required decimal Amount { get; set; }
}
