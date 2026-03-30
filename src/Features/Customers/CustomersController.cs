using Microsoft.AspNetCore.Mvc;
using Backend.Features.Common.Controllers;

namespace Backend.Features.Customers;

[Route("api/customers")]
public class CustomersController(ICustomerService customerService) : BaseController
{
    private readonly ICustomerService _customerService = customerService;

    [HttpPost("register")]
    public async Task<ActionResult<CustomerDto>> RegisterCustomer(CreateCustomerDto dto)
    {
        var customer = await _customerService.CreateCustomerAsync(dto);
        return CreatedAtAction(nameof(GetCustomer), new { id = customer.Id }, customer);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CustomerDto>> GetCustomer(int id)
    {
        return await ExecuteAsync(async () => await _customerService.GetCustomerAsync(id));
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CustomerDto>>> GetAllCustomers()
    {
        return await ExecuteAsync(async () => await _customerService.GetAllCustomersAsync());
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCustomer(int id, CreateCustomerDto dto)
    {
        return await ExecuteAsync(async () => 
        {
            await _customerService.UpdateCustomerAsync(id, dto);
            return NoContentResponse();
        });
    }
}
