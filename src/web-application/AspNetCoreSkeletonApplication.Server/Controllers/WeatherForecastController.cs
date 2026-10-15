using AspNetCoreSkeletonApplication.Server.Models;
using AspNetCoreSkeletonApplication.Server.Services.Mapping;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AspNetCoreSkeletonApplication.Server.Controllers;

[ApiController]
[Route("[controller]")]
[AllowAnonymous]
public class WeatherForecastController(WeatherForecastMappingService mappingService) : ControllerBase
{
    [HttpGet(Name = "GetWeatherForecast")]
    public IReadOnlyList<WeatherForecastModel> Get() => mappingService.List();
}
