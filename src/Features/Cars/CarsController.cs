using Microsoft.AspNetCore.Mvc;

namespace Backend.Features.Cars;

[ApiController]
[Route("api/cars")]
public class CarsController(ICarService carService) : ControllerBase
{
    private readonly ICarService _carService = carService;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CarDto>>> GetAllCars()
    {
        try
        {
            var cars = await _carService.GetAllCarsAsync();
            return Ok(cars);
        }
        catch (Exception)
        {
            return BadRequest("Unable to retrieve cars");
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CarDto>> GetCar(int id)
    {
        try
        {
            var car = await _carService.GetCarAsync(id);
            return Ok(car);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (Exception)
        {
            return BadRequest("Unable to retrieve car");
        }
    }

    [HttpPost]
    public async Task<ActionResult<CarDto>> CreateCar(CreateCarDto dto)
    {
        try
        {
            var car = await _carService.CreateCarAsync(dto);
            return CreatedAtAction(nameof(GetCar), new { id = car.Id }, car);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCar(int id)
    {
        try
        {
            await _carService.DeleteCarAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("{id}/entry")]
    public async Task<ActionResult<CarDto>> RecordEntry(int id)
    {
        try
        {
            var car = await _carService.UpdateCarEntryAsync(id);
            return Ok(car);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("{id}/exit")]
    public async Task<ActionResult<CarDto>> RecordExit(int id)
    {
        try
        {
            var car = await _carService.UpdateCarExitAsync(id);
            return Ok(car);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
