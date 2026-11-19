using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WeatherApp.Application.Weather;
using WeatherApp.Infrastructure.Weather;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddMemoryCache();

        services.AddHttpClient("Weather", c =>
        {
            c.BaseAddress = new Uri("https://api.open-meteo.com/");
            c.Timeout = TimeSpan.FromSeconds(60);
        }).AddStandardHedgingHandler();

        services.AddHttpClient("Geocoding", c =>
        {
            c.BaseAddress = new Uri("https://geocoding-api.open-meteo.com/");
            c.Timeout = TimeSpan.FromSeconds(60);
        }).AddStandardHedgingHandler();

        services.AddScoped<WeatherClient>();
        services.AddScoped<IWeatherProvider>(sp => new CachedWeatherProvider(
            sp.GetRequiredService<WeatherClient>(),
            sp.GetRequiredService<IMemoryCache>(),
            sp.GetRequiredService<ILogger<CachedWeatherProvider>>()
        ));

        return services;
    }
}