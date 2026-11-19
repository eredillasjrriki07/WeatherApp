namespace WeatherApp.Domain.Entities;

public sealed record WeatherSnapshot(
    string City,
    string Country,
    double TemperatureC,
    double WindSpeedKmh,
    int HumidityPercent,
    int WeatherCode,
    DateTime ObserveredAtUtc
);
