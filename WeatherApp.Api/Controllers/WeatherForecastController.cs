using Microsoft.AspNetCore.Mvc;
using WeatherApp.Api.Model;
using WeatherApp.Application.Weather;

namespace WeatherApp.Api.Controllers;

[ApiController]
[Route("weather")]
public class WeatherForecastController(GetCurrentWeatherHandler _weatherHandler) : ControllerBase
{
    [HttpGet("{city}")]
    [ProducesResponseType(typeof(WeatherResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WeatherResponse>> Get(string city, CancellationToken ct)
    {
        var snapshot = await _weatherHandler.HandleAsync(city, ct);
        return Ok(WeatherResponse.From(snapshot));
    }
}
