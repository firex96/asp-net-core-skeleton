using AspNetCoreSkeletonApplication.Server.Weather.Dtos;

namespace AspNetCoreSkeletonApplication.Server.Weather.Services.Biz;

/// <summary>DB 를 쓰지 않는 앱 전용 샘플. 오늘 이후 5일의 임의 예보를 만든다.</summary>
public class WeatherForecastBizService(TimeProvider timeProvider)
{
    private static readonly string[] Summaries =
    [
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    ];

    public IReadOnlyList<WeatherForecastDto> List()
    {
        var today = DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);

        return Enumerable.Range(1, 5)
            .Select(index => new WeatherForecastDto(
                today.AddDays(index),
                Random.Shared.Next(-20, 55),
                Summaries[Random.Shared.Next(Summaries.Length)]))
            .ToArray();
    }
}
