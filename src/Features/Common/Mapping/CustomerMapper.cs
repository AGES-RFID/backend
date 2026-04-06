using Backend.Features.Customers.Dtos;
using Backend.Features.Customers.Models;

namespace Backend.Features.Common.Mapping;

public static class CustomerMapper
{
    public static CustomerDto ToDto(this Customer customer)
    {
        return new CustomerDto
        {
            Id = customer.UserId,
            Email = customer.Email,
            Name = customer.Name,
            Cpf = customer.Cpf,
            Cellphone = customer.Cellphone,
            Cep = customer.Cep,
            Address = customer.Address,
            CreatedAt = customer.CreatedAt,
            UpdatedAt = customer.UpdatedAt
        };
    }

    public static Customer ToEntity(this CreateCustomerDto dto)
    {
        return new Customer
        {
            Email = dto.Email,
            Name = dto.Name,
            Cpf = dto.Cpf,
            Cellphone = dto.Cellphone,
            Cep = dto.Cep,
            Address = dto.Address
        };
    }

    public static void UpdateEntity(this Customer customer, CreateCustomerDto dto)
    {
        customer.Email = dto.Email;
        customer.Name = dto.Name;
        customer.Cpf = dto.Cpf;
        customer.Cellphone = dto.Cellphone;
        customer.Cep = dto.Cep;
        customer.Address = dto.Address;
        customer.UpdatedAt = DateTime.UtcNow;
    }

    public static IEnumerable<CustomerDto> ToDtos(this IEnumerable<Customer> customers)
    {
        return customers.Select(customer => customer.ToDto());
    }
}
