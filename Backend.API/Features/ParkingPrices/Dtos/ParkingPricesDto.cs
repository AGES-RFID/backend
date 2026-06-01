namespace Backend.Features.ParkingPrices;

public class ParkingPricesDto
{
    public Guid ParkingPriceId { get; init; }
    public int ToleranceMinutes { get; init; }
    public decimal BasePrice { get; init; }
    public int ThresholdMinutes { get; init; }
    public int MaxOccupancy { get; init; }
    public decimal HourlyRate { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }

    public static ParkingPricesDto FromModel(ParkingPrice prices) => new()
    {
        ParkingPriceId = prices.ParkingPriceId,
        ToleranceMinutes = prices.ToleranceMinutes,
        BasePrice = prices.BasePrice,
        ThresholdMinutes = prices.ThresholdMinutes,
        MaxOccupancy = prices.MaxOccupancy,
        HourlyRate = prices.HourlyRate,
        CreatedAt = prices.CreatedAt,
        UpdatedAt = prices.UpdatedAt
    };
}
