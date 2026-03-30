namespace Backend.Features.Customers.Dtos;

public class CustomerDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Cpf { get; set; } = string.Empty;
    public string Cellphone { get; set; } = string.Empty;
    public string Cep { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
