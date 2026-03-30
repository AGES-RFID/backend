using Backend.Features.Common.Mapping;

namespace Backend.Features.Cars;

public interface ICarService
{
    Task<CarDto> CreateCarAsync(CreateCarDto dto);
    Task<CarDto> GetCarAsync(int id);
    Task<IEnumerable<CarDto>> GetAllCarsAsync();
    Task DeleteCarAsync(int id);
    Task<CarDto> UpdateCarEntryAsync(int id);
    Task<CarDto> UpdateCarExitAsync(int id);
}

public class CarService : ICarService
{
    private readonly List<Backend.Features.Cars.Models.Car> _cars = new();

    public async Task<CarDto> CreateCarAsync(CreateCarDto dto)
    {
        if (_cars.Any(c => c.PlateNumber == dto.PlateNumber))
            throw new InvalidOperationException($"Carro com placa {dto.PlateNumber} já existe");

        var car = dto.ToEntity();
        car.CarId = _cars.Count + 1;

        _cars.Add(car);

        return car.ToDto();
    }

    public async Task<CarDto> GetCarAsync(int id)
    {
        var car = _cars.FirstOrDefault(c => c.CarId == id);
        if (car == null)
            throw new KeyNotFoundException($"Carro com ID {id} não encontrado");

        return car.ToDto();
    }

    public async Task<IEnumerable<CarDto>> GetAllCarsAsync()
    {
        return _cars.ToDtos();
    }

    public async Task DeleteCarAsync(int id)
    {
        var car = _cars.FirstOrDefault(c => c.CarId == id);
        if (car == null)
            throw new KeyNotFoundException($"Carro com ID {id} não encontrado");

        _cars.Remove(car);
    }

    public async Task<CarDto> UpdateCarEntryAsync(int id)
    {
        var car = _cars.FirstOrDefault(c => c.CarId == id);
        if (car == null)
            throw new KeyNotFoundException($"Carro com ID {id} não encontrado");

        car.RecordEntry();

        return car.ToDto();
    }

    public async Task<CarDto> UpdateCarExitAsync(int id)
    {
        var car = _cars.FirstOrDefault(c => c.CarId == id);
        if (car == null)
            throw new KeyNotFoundException($"Carro com ID {id} não encontrado");

        if (!car.LastEntry.HasValue)
            throw new InvalidOperationException($"Carro com ID {id} não possui registro de entrada");

        car.RecordExit();

        return car.ToDto();
    }
}
