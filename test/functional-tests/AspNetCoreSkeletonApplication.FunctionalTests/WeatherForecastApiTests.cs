using System.Net.Http.Json;
using AspNetCoreSkeletonApplication.Server;

namespace AspNetCoreSkeletonApplication.FunctionalTests;

public class WeatherForecastApiTests(SkeletonWebApplicationFactory factory) : IClassFixture<SkeletonWebApplicationFactory>
{
    [Fact]
    public async Task Get_ReturnsFiveForecasts()
    {
        var client = factory.CreateClient();

        var forecasts = await client.GetFromJsonAsync<WeatherForecast[]>("/weatherforecast", TestContext.Current.CancellationToken);

        Assert.NotNull(forecasts);
        Assert.Equal(5, forecasts.Length);
    }

    [Fact]
    public async Task OpenApiDocument_DescribesWeatherForecast()
    {
        var client = factory.CreateClient();

        var document = await client.GetStringAsync("/openapi/v1.json", TestContext.Current.CancellationToken);

        Assert.Contains("/WeatherForecast", document);
    }
}
