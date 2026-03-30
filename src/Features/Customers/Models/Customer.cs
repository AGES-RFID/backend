using Backend.Features.Users;

namespace Backend.Features.Customers.Models;

public class Customer : User
{
    public required string Cpf { get; set; }
    public required string Cellphone { get; set; }
    public required string Cep { get; set; }
    public required string Address { get; set; }
}
