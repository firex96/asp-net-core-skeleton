using System.Net.Http.Json;
using System.Text.Json;
using AspNetCoreSkeletonApplication.Server.Weather.Models;

namespace AspNetCoreSkeletonApplication.FunctionalTests;

public class WeatherForecastApiTests(SkeletonWebApplicationFactory factory) : IClassFixture<SkeletonWebApplicationFactory>
{
    [Fact]
    public async Task Get_ReturnsFiveForecastsWithFahrenheit()
    {
        var client = factory.CreateClient();

        var forecasts = await client.GetFromJsonAsync<WeatherForecastModel[]>("/weatherforecast", TestContext.Current.CancellationToken);

        Assert.NotNull(forecasts);
        Assert.Equal(5, forecasts.Length);
        Assert.All(forecasts, forecast => Assert.Equal(32 + (int)(forecast.TemperatureC / 0.5556), forecast.TemperatureF));
    }

    [Fact]
    public async Task Get_KeepsJsonShapeTheSpaClientsRead()
    {
        var client = factory.CreateClient();

        using var document = JsonDocument.Parse(
            await client.GetStringAsync("/weatherforecast", TestContext.Current.CancellationToken));

        var names = document.RootElement[0].EnumerateObject().Select(property => property.Name).ToArray();
        Assert.Equal(["date", "temperatureC", "temperatureF", "summary"], names);
    }

    [Fact]
    public async Task OpenApiDocument_DescribesWeatherForecast()
    {
        var client = factory.CreateClient();

        var document = await client.GetStringAsync("/openapi/v1.json", TestContext.Current.CancellationToken);

        Assert.Contains("/WeatherForecast", document);
    }
}
