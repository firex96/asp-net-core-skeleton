using AspNetCoreSkeletonApplication.Server.Models;
using AspNetCoreSkeletonApplication.Server.Services.Mapping;
using Microsoft.AspNetCore.Mvc;

namespace AspNetCoreSkeletonApplication.Server.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController(WeatherForecastMappingService mappingService) : ControllerBase
{
    [HttpGet(Name = "GetWeatherForecast")]
    public IReadOnlyList<WeatherForecastModel> Get() => mappingService.List();
}
