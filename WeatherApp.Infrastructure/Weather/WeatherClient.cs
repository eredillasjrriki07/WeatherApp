using System.Net.Http.Json;
using System.Text.Json.Serialization;
using WeatherApp.Application.Weather;
using WeatherApp.Domain.Entities;

namespace WeatherApp.Infrastructure.Weather;

public sealed class WeatherClient(
    IHttpClientFactory _httpClientFactory
) : IWeatherProvider
{
    public async Task<WeatherSnapshot?> GetCurrentAsync(string city, CancellationToken ct = default)
    {
        // 1. City -> coordinates
        var geo = _httpClientFactory.CreateClient("Geocoding");
        var geoResponse = await geo.GetFromJsonAsync<GeocodingResponse>($"v1/search?name={Uri.EscapeDataString(city)}&count=1", ct);

        var place = geoResponse?.Results?.FirstOrDefault();
        if (place is null) return null;

        // 2. Coordinates -> current weather
        var weather = _httpClientFactory.CreateClient("Weather");
        var url = $"v1/forecast?latitude={place.Latitude}&longitude={place.Longitude}&current=temperature_2m,relative_humidity_2m,wind_speed_10m,weather_code";

        var forecast = await weather.GetFromJsonAsync<ForecastResponse>(url, ct);
        if (forecast?.Current is null) return null;

        var c = forecast.Current;
        return new WeatherSnapshot(
            place.Name,
            place.Country ?? "",
            c.Temperature,
            c.WindSpeed,
            c.Humidity,
            c.WeatherCode,
            DateTime.UtcNow
        );
    }

    // External JSON shapes, private so they never leak out of Infrastructure
    private sealed record GeocodingResponse(
        [property: JsonPropertyName("results")] List<GeoResult>? Results);

    private sealed record GeoResult(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("country")] string? Country,
        [property: JsonPropertyName("latitude")] double Latitude,
        [property: JsonPropertyName("longitude")] double Longitude);

    private sealed record ForecastResponse(
        [property: JsonPropertyName("current")] CurrentWeather? Current);

    private sealed record CurrentWeather(
        [property: JsonPropertyName("temperature_2m")] double Temperature,
        [property: JsonPropertyName("relative_humidity_2m")] int Humidity,
        [property: JsonPropertyName("wind_speed_10m")] double WindSpeed,
        [property: JsonPropertyName("weather_code")] int WeatherCode);
}
