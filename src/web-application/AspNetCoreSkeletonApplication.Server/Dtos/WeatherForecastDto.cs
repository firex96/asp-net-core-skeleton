namespace AspNetCoreSkeletonApplication.Server.Dtos;

public sealed record WeatherForecastDto(DateOnly Date, int TemperatureC, string? Summary);
