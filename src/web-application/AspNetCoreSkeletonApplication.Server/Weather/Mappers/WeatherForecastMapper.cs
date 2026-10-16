using AspNetCoreSkeletonApplication.Server.Weather.Dtos;
using AspNetCoreSkeletonApplication.Server.Weather.Models;
using Riok.Mapperly.Abstractions;

namespace AspNetCoreSkeletonApplication.Server.Weather.Mappers;

[Mapper]
public static partial class WeatherForecastMapper
{
    /// <summary>계산된 값은 MappingService 가 추가 매개변수로 넘긴다 — 매퍼는 이름이 같은 값을 옮기기만 한다.</summary>
    public static partial WeatherForecastModel ToModel(this WeatherForecastDto dto, int temperatureF);
}
