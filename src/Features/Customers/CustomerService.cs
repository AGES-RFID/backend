using Backend.Features.Common.Mapping;
using Backend.Features.Common.Services;

namespace Backend.Features.Customers;

public interface ICustomerService
{
    Task<CustomerDto> CreateCustomerAsync(CreateCustomerDto dto);
    Task<CustomerDto> GetCustomerAsync(int id);
    Task<CustomerDto> UpdateCustomerAsync(int id, CreateCustomerDto dto);
    Task<IEnumerable<CustomerDto>> GetAllCustomersAsync();
}

public class CustomerService : BaseService, ICustomerService
{
    private readonly List<Backend.Features.Customers.Models.Customer> _customers = new();

    public async Task<CustomerDto> CreateCustomerAsync(CreateCustomerDto dto)
    {
        var customer = dto.ToEntity();
        customer.UserId = GenerateId(_customers);

        _customers.Add(customer);

        return customer.ToDto();
    }

    public async Task<CustomerDto> GetCustomerAsync(int id)
    {
        var customer = _customers.FirstOrDefault(c => c.UserId == id);
        ValidateNotNull(customer, "Cliente", id);

        return customer.ToDto();
    }

    public async Task<CustomerDto> UpdateCustomerAsync(int id, CreateCustomerDto dto)
    {
        var customer = _customers.FirstOrDefault(c => c.UserId == id);
        ValidateNotNull(customer, "Cliente", id);

        customer.UpdateEntity(dto);

        return customer.ToDto();
    }

    public async Task<IEnumerable<CustomerDto>> GetAllCustomersAsync()
    {
        return _customers.ToDtos();
    }
}
