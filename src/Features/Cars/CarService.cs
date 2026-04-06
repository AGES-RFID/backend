using Backend.Features.Common.Mapping;
using Backend.Features.Common.Services;

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

public class CarService : BaseService, ICarService
{
    private readonly List<Backend.Features.Cars.Models.Car> _cars = new();

    public async Task<CarDto> CreateCarAsync(CreateCarDto dto)
    {
        var existingCar = _cars.FirstOrDefault(c => c.PlateNumber == dto.PlateNumber);
        ValidateExists(existingCar, "Carro com placa", dto.PlateNumber);

        var car = dto.ToEntity();
        car.CarId = GenerateId(_cars);

        _cars.Add(car);

        return car.ToDto();
    }

    public async Task<CarDto> GetCarAsync(int id)
    {
        var car = _cars.FirstOrDefault(c => c.CarId == id);
        ValidateNotNull(car, "Carro", id);

        return car.ToDto();
    }

    public async Task<IEnumerable<CarDto>> GetAllCarsAsync()
    {
        return _cars.ToDtos();
    }

    public async Task DeleteCarAsync(int id)
    {
        var car = _cars.FirstOrDefault(c => c.CarId == id);
        ValidateNotNull(car, "Carro", id);

        _cars.Remove(car);
    }

    public async Task<CarDto> UpdateCarEntryAsync(int id)
    {
        var car = _cars.FirstOrDefault(c => c.CarId == id);
        ValidateNotNull(car, "Carro", id);

        car.RecordEntry();

        return car.ToDto();
    }

    public async Task<CarDto> UpdateCarExitAsync(int id)
    {
        var car = _cars.FirstOrDefault(c => c.CarId == id);
        ValidateNotNull(car, "Carro", id);

        ValidateHasEntry(car.LastEntry, "Carro", id);

        car.RecordExit();

        return car.ToDto();
    }
}
