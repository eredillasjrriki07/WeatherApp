# Weather Dashboard API

A small ASP.NET Core Web API that returns the current weather for a city. It calls the free [Open-Meteo](https://open-meteo.com/) API (no API key needed), maps the response into its own types, and caches results for 10 minutes so repeated requests don't hit the external service.

## Features

- `GET /weather/{city}` returns current temperature, wind speed, humidity and weather code
- Calls Open-Meteo through named `HttpClient`s created by `IHttpClientFactory`, each with a base URL and timeout
- Maps external JSON into its own types, so Open-Meteo's field names never leak into the app
- 10-minute in-memory cache using `IMemoryCache`, implemented as a decorator
- Retries, circuit breaker and timeouts via `AddStandardResilienceHandler()`
- Clean 404 `ProblemDetails` response for unknown cities
- Interactive API docs with Scalar

## Tech stack

- .NET 9 / ASP.NET Core (controllers)
- `IHttpClientFactory` with `Microsoft.Extensions.Http.Resilience`
- `IMemoryCache`
- Built-in OpenAPI + [Scalar](https://github.com/scalar/scalar)
- Clean Architecture

## Architecture

```
WeatherApp.Api             Controllers, Program.cs, exception handler, response DTO
WeatherApp.Application     IWeatherProvider (port), GetCurrentWeatherHandler, NotFoundException
WeatherApp.Domain          WeatherSnapshot (value object)
WeatherApp.Infrastructure  WeatherClient, CachedWeatherProvider, DI registration
```

Dependencies point inward:

```
Api ──────────► Application ──► Domain
 └──► Infrastructure ──► Application
```

The Application layer defines what it needs (`IWeatherProvider`) and Infrastructure provides it. The business logic never depends on HTTP, JSON or Open-Meteo.

### Request flow

```
Controller → Handler → CachedWeatherProvider → WeatherClient → Open-Meteo
```

1. `WeatherController` receives `GET /weather/{city}`.
2. `GetCurrentWeatherHandler` asks `IWeatherProvider` for the weather and throws `NotFoundException` if there is none.
3. `CachedWeatherProvider` returns a cached result if there is one (HIT). Otherwise it calls the inner provider (MISS) and stores the result for 10 minutes.
4. `WeatherClient` resolves the city to coordinates with the geocoding API, then fetches the current weather and maps it to `WeatherSnapshot`.

## Getting started

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download)

### Run

```bash
git clone <your-repo-url>
cd <your-repo-folder>
dotnet run --project WeatherApp.Api
```

The URL and port are set in `WeatherApp.Api/Properties/launchSettings.json`.

### API docs

In Development, open:

```
https://localhost:<port>/scalar/v1
```

The raw OpenAPI document is served at `/openapi/v1.json`.

## API

### `GET /weather/{city}`

```bash
curl https://localhost:<port>/weather/Manila
```

**200 OK**

```json
{
  "city": "Manila",
  "country": "Philippines",
  "temperatureC": 31.2,
  "windSpeedKmh": 12.4,
  "humidityPercent": 74,
  "weatherCode": 2,
  "observedAtUtc": "2026-11-19T06:30:00Z"
}
```

**404 Not Found** (unknown city)

```json
{
  "status": 404,
  "title": "Not found",
  "detail": "City 'Atlantisss' not found."
}
```

`weatherCode` is the [WMO weather code](https://open-meteo.com/en/docs) returned by Open-Meteo (0 = clear sky, 61 = light rain, and so on).

## Verifying the cache

The `HttpClient` pipeline logs outgoing requests at Information level, so the console shows whether the external API was called.

Call the same city twice within 10 minutes:

```
First call:   Cache MISS for Manila, calling external API
              Sending HTTP request GET https://geocoding-api.open-meteo.com/...
              Sending HTTP request GET https://api.open-meteo.com/...
Second call:  Cache HIT for Manila
```

On the second call there are no outgoing HTTP log lines, so the external API was not hit.

Notes:
- City names are trimmed and lowercased for the cache key, so `Manila` and `manila` share one entry.
- "City not found" results are not cached.

## Configuration

Registered in `WeatherApp.Infrastructure/DependencyInjection.cs`:

| Setting | Value |
|---|---|
| Forecast base URL | `https://api.open-meteo.com/` |
| Geocoding base URL | `https://geocoding-api.open-meteo.com/` |
| Cache duration | 10 minutes |
| `HttpClient.Timeout` | 60 seconds (must exceed the resilience handler's total timeout) |

`AddStandardResilienceHandler()` adds retries, a circuit breaker, and per-attempt and total timeouts. These can be tuned through its options.

## Design notes

- **Decorator for caching.** `CachedWeatherProvider` and `WeatherClient` both implement `IWeatherProvider`, and the cache wraps the real client. The handler never knows the cache exists.
- **Manual DI wiring.** Because two classes implement the same interface and one wraps the other, `IWeatherProvider` is registered with a factory so the container gets the right inner provider.
- **Private JSON models.** Open-Meteo's response shapes are private records inside `WeatherClient`.
- **Nothing is persisted.** There is no database. Everything lives in memory and is lost on restart.

## Possible next steps

- Background service that keeps the cache warm for chosen cities
- Unit tests for `GetCurrentWeatherHandler` and `CachedWeatherProvider` using a fake `IWeatherProvider`
- Forecast endpoint (`/weather/{city}/forecast`)
- Switch to `IDistributedCache` (Redis) if the API ever runs on multiple instances
