namespace AspNetCoreSkeletonApplication.Server.Weather.Models;

public sealed record WeatherForecastModel(DateOnly Date, int TemperatureC, int TemperatureF, string? Summary);
