using WeatherApp.Domain.Entities;

namespace WeatherApp.Api.Model;

public sealed record WeatherResponse(
    string City,
    string Country,
    double TemperatureC,
    double WindSpeedKmh,
    int HumidityPercent,
    int WeatherCode,
    DateTime ObserveredAtUtc
)
{
    public static WeatherResponse From(WeatherSnapshot ws) =>
        new(ws.City, ws.Country, ws.TemperatureC, ws.WindSpeedKmh, ws.HumidityPercent, ws.WeatherCode, ws.ObserveredAtUtc);
}
