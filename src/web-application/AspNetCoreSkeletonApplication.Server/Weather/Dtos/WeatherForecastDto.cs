namespace AspNetCoreSkeletonApplication.Server.Weather.Dtos;

public sealed record WeatherForecastDto(DateOnly Date, int TemperatureC, string? Summary);
