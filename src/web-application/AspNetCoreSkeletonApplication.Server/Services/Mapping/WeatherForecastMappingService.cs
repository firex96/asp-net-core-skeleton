using AspNetCoreSkeletonApplication.Server.Mappers;
using AspNetCoreSkeletonApplication.Server.Models;
using AspNetCoreSkeletonApplication.Server.Services.Biz;

namespace AspNetCoreSkeletonApplication.Server.Services.Mapping;

/// <summary>
/// 화면에 필요한 변환을 맡는다. 화씨는 표시용 파생 값이라 여기서 계산한다 — 매퍼는 필드를 옮기기만 한다.
/// </summary>
public class WeatherForecastMappingService(WeatherForecastBizService bizService)
{
    public IReadOnlyList<WeatherForecastModel> List() =>
        bizService.List()
            .Select(forecast => forecast.ToModel(temperatureF: 32 + (int)(forecast.TemperatureC / 0.5556)))
            .ToArray();
}
