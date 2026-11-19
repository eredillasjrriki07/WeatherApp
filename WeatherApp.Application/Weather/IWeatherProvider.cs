using WeatherApp.Domain.Entities;

namespace WeatherApp.Application.Weather;

public interface IWeatherProvider
{
    Task<WeatherSnapshot?> GetCurrentAsync(string city, CancellationToken ct = default);
}
