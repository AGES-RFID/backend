using Backend.Features.Common.Mapping;

namespace Backend.Features.Customers;

public interface ICustomerService
{
    Task<CustomerDto> CreateCustomerAsync(CreateCustomerDto dto);
    Task<CustomerDto> GetCustomerAsync(int id);
    Task<CustomerDto> UpdateCustomerAsync(int id, CreateCustomerDto dto);
    Task<IEnumerable<CustomerDto>> GetAllCustomersAsync();
}

public class CustomerService : ICustomerService
{
    private readonly List<Backend.Features.Customers.Models.Customer> _customers = new();

    public async Task<CustomerDto> CreateCustomerAsync(CreateCustomerDto dto)
    {
        var customer = dto.ToEntity();
        customer.UserId = _customers.Count + 1;

        _customers.Add(customer);

        return customer.ToDto();
    }

    public async Task<CustomerDto> GetCustomerAsync(int id)
    {
        var customer = _customers.FirstOrDefault(c => c.UserId == id);
        if (customer == null)
            throw new KeyNotFoundException($"Cliente com ID {id} não encontrado");

        return customer.ToDto();
    }

    public async Task<CustomerDto> UpdateCustomerAsync(int id, CreateCustomerDto dto)
    {
        var customer = _customers.FirstOrDefault(c => c.UserId == id);
        if (customer == null)
            throw new KeyNotFoundException($"Cliente com ID {id} não encontrado");

        customer.UpdateEntity(dto);

        return customer.ToDto();
    }

    public async Task<IEnumerable<CustomerDto>> GetAllCustomersAsync()
    {
        return _customers.ToDtos();
    }
}
