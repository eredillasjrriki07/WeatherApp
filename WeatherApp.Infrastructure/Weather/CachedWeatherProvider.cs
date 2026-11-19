using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using WeatherApp.Application.Weather;
using WeatherApp.Domain.Entities;

namespace WeatherApp.Infrastructure.Weather;

public sealed class CachedWeatherProvider(
    IWeatherProvider _inner,
    IMemoryCache _cache,
    ILogger<CachedWeatherProvider> _logger
) : IWeatherProvider
{
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(10);

    public async Task<WeatherSnapshot?> GetCurrentAsync(string city, CancellationToken ct = default)
    {
        var key = $"weather:{city.Trim().ToLowerInvariant()}";

        if (_cache.TryGetValue(key, out WeatherSnapshot? hit))
        {
            _logger.LogInformation("Cache HIT for {City}", city);
            return hit;
        }

        _logger.LogInformation("Cache MISS for {City}, calling internal API", city);
        var result = await _inner.GetCurrentAsync(city, ct);

        if (result is not null)
            _cache.Set(key, result, Ttl);

        return result;
    }
}
