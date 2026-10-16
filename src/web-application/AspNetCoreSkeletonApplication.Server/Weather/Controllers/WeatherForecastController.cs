using AspNetCoreSkeletonApplication.Server.Weather.Models;
using AspNetCoreSkeletonApplication.Server.Weather.Services.Mapping;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AspNetCoreSkeletonApplication.Server.Weather.Controllers;

[ApiController]
[Route("[controller]")]
[AllowAnonymous]
public class WeatherForecastController(WeatherForecastMappingService mappingService) : ControllerBase
{
    [HttpGet(Name = "GetWeatherForecast")]
    public IReadOnlyList<WeatherForecastModel> Get() => mappingService.List();
}
