namespace Backend.Features.Accesses;

public class TimeseriesResponseDto
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public required IEnumerable<TimeSeriesDto> Series { get; set; }
}

public class TimeSeriesDto
{
    public required string Key { get; set; }
    public required IEnumerable<TimeSeriesPointDto> Points { get; set; }
}

public class TimeSeriesPointDto
{
    public DateTime Timestamp { get; set; }
    public int Count { get; set; }
}
