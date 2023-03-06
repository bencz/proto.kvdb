using Microsoft.AspNetCore.Mvc;

namespace Proto.KvDb.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WeatherForecastController : ControllerBase
{
    private readonly ILogger<WeatherForecastController> _logger;

    public WeatherForecastController(ILogger<WeatherForecastController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public async Task<object> GetCoisa()
    {
        await Task.Delay(1);
        return new
        {
            X = 1
        };
    }
}