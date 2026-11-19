using WeatherApp.Application.Common.Exceptions;
using WeatherApp.Domain.Entities;

namespace WeatherApp.Application.Weather;

public sealed class GetCurrentWeatherHandler(
    IWeatherProvider _weatherProvider
)
{
    public async Task<WeatherSnapshot> HandleAsync(string city, CancellationToken ct) =>
        await _weatherProvider.GetCurrentAsync(city, ct)
            ?? throw new NotFoundException($"City '{city}' not found!");
}
