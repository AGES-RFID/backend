using Backend.Features.Cars.Dtos;
using Backend.Features.Cars.Models;

namespace Backend.Features.Common.Mapping;

public static class CarMapper
{
    public static CarDto ToDto(this Car car)
    {
        // Calculate duration if car is currently in parking lot
        TimeSpan? duration = null;
        if (car.LastEntry.HasValue && !car.LastExit.HasValue)
        {
            duration = DateTime.UtcNow - car.LastEntry.Value;
        }
        else if (car.DurationInParkingLot.HasValue)
        {
            duration = car.DurationInParkingLot;
        }

        return new CarDto
        {
            Id = car.CarId,
            PlateNumber = car.PlateNumber,
            CustomerId = car.CustomerId,
            RfidTagId = car.RfidTagId,
            LastEntry = car.LastEntry,
            LastExit = car.LastExit,
            DurationInParkingLot = duration,
            CreatedAt = car.CreatedAt,
            UpdatedAt = car.UpdatedAt
        };
    }

    public static Car ToEntity(this CreateCarDto dto)
    {
        return new Car
        {
            PlateNumber = dto.PlateNumber,
            CustomerId = dto.CustomerId,
            RfidTagId = dto.RfidTagId
        };
    }

    public static void UpdateEntity(this Car car, CreateCarDto dto)
    {
        car.PlateNumber = dto.PlateNumber;
        car.CustomerId = dto.CustomerId;
        car.RfidTagId = dto.RfidTagId;
        car.UpdatedAt = DateTime.UtcNow;
    }

    public static void RecordEntry(this Car car)
    {
        car.LastEntry = DateTime.UtcNow;
        car.LastExit = null;
        car.DurationInParkingLot = null;
        car.UpdatedAt = DateTime.UtcNow;
    }

    public static void RecordExit(this Car car)
    {
        car.LastExit = DateTime.UtcNow;
        if (car.LastEntry.HasValue)
        {
            car.DurationInParkingLot = car.LastExit.Value - car.LastEntry.Value;
        }
        car.UpdatedAt = DateTime.UtcNow;
    }

    public static IEnumerable<CarDto> ToDtos(this IEnumerable<Car> cars)
    {
        return cars.Select(car => car.ToDto());
    }
}
